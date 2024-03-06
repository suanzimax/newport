using System.Diagnostics;
using EpicsSharp.ChannelAccess.Client;
using EpicsSharp.ChannelAccess.Server;

// 发布pv,创建一个 EPICS Channel Access 服务器
var server = new CAServer();
// 创建一个 double 类型的 PV，名称为 MY_PV，初始值为 0.0
var kai0 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("clapa:Shot_Sshutter:OPEN");
var guan0 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("clapa:Shot_Sshutter:CLOSE");
var restart0 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("clapa:Shot_Sshutter:RESTART");
var chufa0 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("CHUFA");
var guilin0 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("clapa:Shot_Sshutter:HOMED");
var zhuangtai0 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("clapa:Shot_Sshutter:STATE");
var wait0 = server.CreateRecord<EpicsSharp.ChannelAccess.Server.RecordTypes.CADoubleRecord>("clapa:Shot_Sshutter:WIDTH");
// 设置pv初值
kai0.Value = 0.0;
guan0.Value = 0.0;
restart0.Value = 0.0;
chufa0.Value = 0;//初值设为1，始终开启触发状态
guilin0.Value = 0;
zhuangtai0.Value = 2;
// 启动 EPICS 服务器
server.Start();

// 初始化进程
CommandInterfaceXPS.XPS m_xps = null;
m_xps = new CommandInterfaceXPS.XPS();
// 打开XPS连接
if (m_xps != null) 
    //Console.WriteLine("Hello, World!");
    m_xps.OpenInstrument("192.168.254.254", 5001, 1000);
if (m_xps != null)
{
    string XPSversion = string.Empty;
    string errorString = string.Empty;
    int result = m_xps.FirmwareVersionGet(out XPSversion, out errorString);
    if (result == 0)
    {
        Console.WriteLine("连接良好");
    }
    m_xps.KillAll(out errorString);
    m_xps.GroupInitialize("Group1",out errorString);
    m_xps.GroupHomeSearch("Group1",out errorString);

}

System.Threading.Thread.Sleep(20000);
Console.WriteLine("初始化结束");


//Console.WriteLine(kai0.GetDouble("KAI"));
var client = new CAClient();
client.Configuration.SearchAddress = "192.168.254.120:5064";
var kai = client.CreateChannel<string>("clapa:Shot_Sshutter:OPEN");
var guan = client.CreateChannel<string>("clapa:Shot_Sshutter:CLOSE");
var chufa = client.CreateChannel<string>("CHUFA");
var guilin = client.CreateChannel<string>("clapa:Shot_Sshutter:HOMED");
var zhuangtai = client.CreateChannel<string>("clapa:Shot_Sshutter:STATE");
var wait = client.CreateChannel<string>("clapa:Shot_Sshutter:WIDTH");
var restart = client.CreateChannel<string>("clapa:Shot_Sshutter:RESTART");
guilin.Put(1); //完成归零后，值置为1
while (true)
{  
    Stopwatch stopwatch = new Stopwatch();
    stopwatch.Start();
    double kaivalue = double.Parse(kai.Get());
    if (kaivalue == 1)
    {
        string errorString = string.Empty;
        //开操作
        m_xps.GroupMoveAbsolute("Group1", [100], 1, out errorString);
        stopwatch.Stop();
        TimeSpan duration = stopwatch.Elapsed;
        Console.WriteLine("开操作时长: " + duration);
        
        kai.Put(0);
    }
    
    double guanvalue = double.Parse(guan.Get());

    if (guanvalue == 1)
    {
        string errorString = string.Empty;
        //关操作
        m_xps.GroupMoveAbsolute("Group1", [-100], 1, out errorString);

        stopwatch.Stop();
        TimeSpan duration = stopwatch.Elapsed;
        Console.WriteLine("关操作时长: " + duration);
        
        guan.Put(0);
    }

    double chufavalue = double.Parse(chufa.Get());
// 触发操作
        string errorString1 = string.Empty;
        double[] b = new double[0];
        m_xps.GPIOAnalogGet(["GPIO4.ADC1"], out b, out errorString1);
        foreach (double V in b)
        {
            Console.WriteLine("当前GPIO值: " + V);
            if (V > 3)
            {
                m_xps.GroupMoveAbsolute("Group1", [100], 1, out errorString1);
                int waitvalue = int.Parse(wait.Get());
                System.Threading.Thread.Sleep(waitvalue); //以ms为计算单位
                m_xps.GroupMoveAbsolute("Group1", [-100], 1, out errorString1);
            }
        }

        double[] a1 = new double[0];
        m_xps.GroupPositionCurrentGet("Group1", out a1, 1, out errorString1);
        foreach (double z in a1)
        {
            Console.WriteLine("当前位置: " + z);
        }
        stopwatch.Stop();
        TimeSpan duration1 = stopwatch.Elapsed;
        Console.WriteLine("触发操作时长: " + duration1);
    
    string errorString0 = string.Empty;
    double[] a0 = new double[0];
    m_xps.GroupPositionCurrentGet("Group1", out a0, 1, out errorString0);
    foreach (double z in a0)
    {
        Console.WriteLine("当前位置: " + z);
        if (z>-100 && z<100)
        {
            if (z>70)
            {zhuangtai.Put(1);}
            else
            {zhuangtai.Put(0);} 
        }
    }

    
    // 重启操作
    double restartvalue = double.Parse(restart.Get());
    if (restartvalue == 1)
    {
        guilin.Put(0); 
        //CommandInterfaceXPS.XPS m_xps = null;
        m_xps = new CommandInterfaceXPS.XPS();
        if (m_xps != null)
            m_xps.OpenInstrument("192.168.254.254", 5001, 1000);
        if (m_xps != null)
        {
            string XPSversion = string.Empty;
            string errorString = string.Empty;
            int result = m_xps.FirmwareVersionGet(out XPSversion, out errorString);
            if (result == 0)
            {
                Console.WriteLine("连接良好1");
            }
            m_xps.KillAll(out errorString);
            m_xps.GroupInitialize("Group1",out errorString);
            m_xps.GroupHomeSearch("Group1",out errorString);
        }
        System.Threading.Thread.Sleep(20000);
        
        if (m_xps != null)
        {
            string XPSversion = string.Empty;
            string errorString = string.Empty;
            int result = m_xps.FirmwareVersionGet(out XPSversion, out errorString);
            if (result == 0)
            {
                Console.WriteLine("连接良好2");
            }
            m_xps.KillAll(out errorString);
            m_xps.GroupInitialize("Group1",out errorString);
            System.Threading.Thread.Sleep(1000);
            m_xps.GroupHomeSearch("Group1",out errorString);
        }
        System.Threading.Thread.Sleep(20000);
        Console.WriteLine("初始化结束");
        
        restart.Put(0);
        guilin.Put(1); 
    }
}

Console.ReadKey();
