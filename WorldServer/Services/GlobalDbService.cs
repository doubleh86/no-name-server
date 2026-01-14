using System.Threading.Channels;
using DbContext.GameDbContext;
using ServerFramework.CommonUtils.Helper;

namespace WorldServer.Services;

/// <summary>
/// 순서 보장을 위해 
/// </summary>
public class GlobalDbService : IDisposable
{
    private CancellationTokenSource _cts;
    private Task[] _workerTasks;
    private int _shardCount;
    private Channel<Func<IGameDbContext, Task>>[] _dbShards;
    private IGameDbContext[] _dbContexts;
    
    private LoggerService _loggerService;
    
    private int _GetShardIndex(long key) => (int)((ulong)key % (ulong)_shardCount);
    public void Initialize(int shardCount, LoggerService loggerService)
    {
        _loggerService = loggerService;
        _shardCount = shardCount;
        
        _cts = new CancellationTokenSource();
        _dbShards = new Channel<Func<IGameDbContext, Task>>[shardCount];
        _dbContexts = new IGameDbContext[shardCount];
        _workerTasks = new Task[shardCount];
        
        for (int i = 0; i < shardCount; i++)
        {
            _dbContexts[i] = MySqlGameDbContext.Create();
            var channelOptions = new BoundedChannelOptions(100000)
                                 {
                                     SingleReader = true,
                                     SingleWriter = false,
                                     FullMode = BoundedChannelFullMode.Wait
                                 };
            _dbShards[i] = Channel.CreateBounded<Func<IGameDbContext, Task>>(channelOptions);
            
            var shardIndex = i;
            _workerTasks[i] = Task.Run(() => _ProcessJobsAsync(shardIndex, _cts.Token));
        }
    }
    
    public void PushJob(long key, Func<IGameDbContext, Task> job)
    {
        int index = _GetShardIndex(key);
        if(_dbShards[index].Writer.TryWrite(job) == false)
            _loggerService.Warning($"Shard {index} is full or drop");
    }

    public async Task PushJobAsync(long key, Func<IGameDbContext, Task> job, CancellationToken cancellationToken = default)
    {
        int idx = _GetShardIndex(key);
        await _dbShards[idx].Writer.WriteAsync(job, cancellationToken);
    }

    public Task PushJobAndWaitAsync(long key, Func<IGameDbContext, Task> job, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int idx = (int)((ulong)key % (ulong)_shardCount);

        Func<IGameDbContext, Task> wrapped = async (dbContext) =>
        {
            try
            {
                await job(dbContext).ConfigureAwait(false);
                tcs.TrySetResult();
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        };

        // 채널 write 실패/취소도 tcs에 반영
        _ = _dbShards[idx].Writer
                          .WriteAsync(wrapped, cancellationToken)
                          .AsTask()
                          .ContinueWith(t =>
                          {
                              if (t.IsCanceled)
                                  tcs.TrySetCanceled(cancellationToken);
                              else if (t.IsFaulted)
                                  tcs.TrySetException(t.Exception!);
                          }, TaskScheduler.Default);

        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        return tcs.Task;
    }


    private async Task _ProcessJobsAsync(int index, CancellationToken token)
    {
        var reader = _dbShards[index].Reader;
        var dbContext = _dbContexts[index];

        try
        {
            while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var job))
                {
                    try
                    {
                        await job(dbContext).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _loggerService.Warning($"Error processing job in shard {index}", e);
                    }
                }
            }
        }
        catch (OperationCanceledException ) { }
        
    }
    
    public void Dispose()
    {
        if (_dbShards != null)
        {
            foreach (var channel in _dbShards)
            {
                channel.Writer.TryComplete();
            }
        }

        try
        {
            if(_workerTasks != null)
                Task.WaitAll(_workerTasks, TimeSpan.FromSeconds(2));
        }
        catch
        {
            _loggerService.Warning("WaitAll Timeout");
        }
        
        _cts?.Cancel();

        if (_dbContexts != null)
        {
            foreach (var dbContext in _dbContexts)
            {
                dbContext.Dispose();
            }    
        }
        
        _cts?.Dispose();
    }
}