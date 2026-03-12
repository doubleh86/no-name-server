using System.Threading.Channels;
using ServerFramework.CommonUtils.Helper;

namespace WorldServer.Services;

public sealed class WorldShardExecutor : IDisposable
{
    private readonly Channel<Func<ValueTask>>[] _channels;
    private readonly Task[] _workers;
    private readonly CancellationTokenSource _cts = new();
    private readonly LoggerService _loggerService;

    public WorldShardExecutor(int shardCount, LoggerService loggerService)
    {
        _loggerService = loggerService;
        _channels = new Channel<Func<ValueTask>>[shardCount];
        _workers = new Task[shardCount];

        for (var i = 0; i < shardCount; i++)
        {
            var options = new BoundedChannelOptions(100000)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            };

            _channels[i] = Channel.CreateBounded<Func<ValueTask>>(options);
            var shardIndex = i;
            _workers[i] = Task.Run(() => ProcessShardAsync(shardIndex, _cts.Token));
        }
    }

    public bool TryEnqueue(int shardIndex, Func<ValueTask> action)
    {
        if (IsInvalidShard(shardIndex))
            return false;

        return _channels[shardIndex].Writer.TryWrite(action);
    }

    public ValueTask EnqueueAsync(int shardIndex, Func<ValueTask> action, CancellationToken cancellationToken = default)
    {
        if (IsInvalidShard(shardIndex))
            throw new ArgumentOutOfRangeException(nameof(shardIndex), shardIndex, "Invalid shard index");

        return _channels[shardIndex].Writer.WriteAsync(action, cancellationToken);
    }

    private async Task ProcessShardAsync(int shardIndex, CancellationToken token)
    {
        var reader = _channels[shardIndex].Reader;

        try
        {
            while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var action))
                {
                    try
                    {
                        await action().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _loggerService.Warning($"World shard job failed: shard={shardIndex}", e);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private bool IsInvalidShard(int shardIndex)
    {
        return shardIndex < 0 || shardIndex >= _channels.Length;
    }

    public void Dispose()
    {
        foreach (var channel in _channels)
        {
            channel.Writer.TryComplete();
        }

        _cts.Cancel();

        try
        {
            Task.WaitAll(_workers, TimeSpan.FromSeconds(2));
        }
        catch
        {
            _loggerService.Warning("WorldShardExecutor worker wait timeout");
        }

        _cts.Dispose();
    }
}
