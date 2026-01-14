// See https://aka.ms/new-console-template for more information

using ClientTest.Handlers;
using ClientTest.Models;

Console.WriteLine("Run Test Client");
Console.WriteLine("1: World Session");
var command = Console.ReadLine();

switch (command)
{
    case "2":
    {
        ThreadPool.SetMinThreads(1000, 1000);
        await RunLoadTest(1);
        return;
    }
}

async Task RunLoadTest(int clientCount)
{
    for (int i = 0; i < clientCount; i++)
    {
        // 1. Task.Run을 사용하여 동기식 Connect 호출을 별도 스레드로 분리 (병렬 실행)
        _ = Task.Run(() =>
        {
            try 
            {
                var tcpClient = new TestSession();
                // 여기서 스레드가 연결될 때까지 점유(Wait)되지만, 
                // Task.Run이므로 메인 루프는 멈추지 않고 다음 i로 넘어갑니다.
                tcpClient.Connect("127.0.0.1", 28080, new WorldServerHandler(tcpClient));
                
                // 연결 성공 후 로직 (동기식이라면 이어서 작성)
                // tcpClient.SendLogin(); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client {i}] Connection failed: {ex.Message}");
            }
        });

        // 2. 서버 Accept 병목 방지를 위해 '메인 루프'에서만 살짝 쉬어줌
        await Task.Delay(100); 

        if ((i + 1) % 100 == 0)
            Console.WriteLine($"[Test] {i + 1} clients creation triggered...");
    }
}