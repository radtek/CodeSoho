﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.OracleClient;
using DevComponents.DotNetBar;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using MonitorAndSet.TopoInfo;
using MonitorAndSet.CommonClass;
using DevComponents.DotNetBar.Controls;
using DevComponents.AdvTree;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using BrawDraw.Com.PhotoFrame.Net.PublicFunctions.Clock;
using System.Configuration;
using System.Xml;
using System.Xml.Linq;
using MonitorAndSet.UILayer;


namespace MonitorAndSet
{
    public partial class MainForm : DevComponents.DotNetBar.Office2007Form
    {

        #region 全局变量和所用资源

        public Mutex mutex = null;// 互斥量 20100601 Ding Yiming+
        private bool doesNotExist = false;

        public static bool topoflag = true; //是否能正确获取拓扑关系的标志 20100203+
        public static bool mflag = false; //是否有未解除告警的标志
        public static String nowPath = ""; //运行的MonitorAndSet.exe所在目录的路径
        public static String strLocalAddrIP = ""; //接收代理数据时绑定的IP地址,从Config.ini中读取的  //20060903+
        public static string ConnectionStr;///数据库连接
        public static IPAddress Multicasrip;///时统IP
        public static int Multicastport;//时统端口

        public static string Community;
        private string ServerURL;//服务器URL                            

        public static Topology m_topo = new Topology();
        private UdpClient server;
        private IPEndPoint groupEP;
        //private static readonly IPAddress GroupAddress = IPAddress.Parse("239.255.0.11"); //从客户端代理接收信息的组播地址
        //private const int GroupPort = 5150;              //从客户端代理接收信息的组播端口号
        //private static IPAddress GroupAddress2 = IPAddress.Parse("239.255.0.2");  //服务器给客户端代理发送指令时的组播地址
        ////private static int GroupPort2 = 13268;      //服务器给客户端代理发送指令时的组播端口号  //20100504 Ding Yiming改为13268

        public static  IPAddress GroupAddress;//从客户端代理接收信息的组播地址
        public static int GroupPort ; //从客户端代理接收信息的组播端口号
        public static IPAddress GroupAddress2;  //服务器给客户端代理发送指令时的组播地址
        public static int GroupPort2 ; //服务器给客户端代理发送指令时的组播端口号  

        private Thread recvInfoFromClient;            //从客户端代理接收信息的线程
        private Thread ping_host;                    //ping主机的线程
        public delegate void InvokeDelegate();      //委托,为了跨线程调用Windows窗体控件
        public delegate void MyDelegate(ListViewEx list1,string st1,string st2,Color color1);///为了更新左侧信息
        public delegate void MyDelegate2(ListViewEx list1, string st1, string st2, string st3, Color color1);///为了更新左侧信息
        public List<MulticastSocket> m_socket = new List<MulticastSocket>();   //接收测量设备数据的SOCKET
        private Thread receive_data;                //从测量设备接收数据的线程          //从视频源接收视频的线程
        private bool threadstate = false;           //线程receive_data是否已经创建并运行的标志

        private Thread init_topo;                 //初始获取拓扑过程的线程      //09-3-3加


        public List<int> m_serverindex = new List<int>(); //记录服务器在m_DrawElement中的索引号,在设置服务器显示位置时使用

        //日志配置参数
        public static String m_LogPath = "";        //日志文件夹保存路径
        public static int loginterval = 1;          //日志文件记录的周期 可取值为1~7
        public static String m_savetime = "Y";      //日志文件保存时间 可取值为Y或M,Y表示保存一年内的,M表示保存一个月内的
        public static int statInterval = 20;   //日志数据统计间隔时间
        //当前时间的年月日
        public int year;
        public int month;
        public int day;

        public String[] yearfoldEntries;      //日志文件夹下的每个年文件夹
        public String[] monthfoldEntries;     //年文件夹下的每个月文件夹
        public String[] fileEntries;          //月文件夹下的每个日志文件

        public String yearstr;    //年文件夹名
        public String monthstr;   //月文件夹名

        public int foldyear;   //文件夹的年
        public int foldmonth;  //文件夹的月

        //当前要显示的某个任务的所有测量设备的集合
        public List<TaskAttribute> m_task = new List<TaskAttribute>();
        public List<int> m_msDevID = new List<int>();  //ID号集合
        public static List<MsDevice> m_msdevice = new List<MsDevice>();
      
        #region 存储所有的机器信息
        /// <summary>
        /// 席位机（按照交换机存储席位机）
        /// </summary>
        public static List<List<HostInfo>> hostlist = new List<List<HostInfo>>();
        /// <summary>
        /// 服务器（按照交换机存储服务器）
        /// </summary>
        public static List<List<HostInfo>> serverlist = new List<List<HostInfo>>();
        
        /*交换机（按顺序）
        */ 
        public static List<HostInfo> switchlist = new List<HostInfo>();
        public static List<int> switchChildCount = new List<int>();
        /// <summary>
        /// 席位机
        /// </summary>
        public static List<HostInfo> hostInfo = new List<HostInfo>();
        /// <summary>
        /// 数据库服务器
        /// </summary>
        public static List<HostInfo> serverInfo = new List<HostInfo>();
        /// <summary>
        /// SAN服务器
        /// </summary>
        public static List<HostInfo> sanInfo = new List<HostInfo>();
        /// <summary>
        /// 交换机
        /// </summary>
        public static List<HostInfo> switchInfo = new List<HostInfo>();
        /// <summary>
        /// 所有的机器
        /// </summary>
        public static List<HostInfo> allInfo = new List<HostInfo>();
        
        #endregion

        public int groupflag = 1;         //标志 当前数据源图标表示的测量设备属于第几组的,取值从1开始
        public int groupcount = 0;        //所有测量设备按6个一组分可分为几组
       
        //2011-08-04 BYG
        public static ListViewEx listViewOutputInfo = new ListViewEx();

        public string[] SeatId;
        //各个对话框背景色
        public static Color DlgBackColor = Color.FromArgb(255, 212, 225, 241);

        //配置参数门限值
        public static int CPUVALUE;        //CPU门限值
        public static int DISKVALUE;       //硬盘门限值
        public static int MEMVALUE;        //内存门限值
        public static int TRAFFICVALUE;    //流量门限值

        //告警声音控制
        public bool IsVoiceOn = true;  
        
        //状态记录时间戳
        public long TimeStamp = 0;
        #region 绘图边界参数
        //指挥控制大厅
        public Point ZHKZDT_LeftUp;        //指挥控制大厅显示区域的左上角点
        public Point ZHKZDT_RightUp;      //指挥控制大厅显示区域的右上角点
        public Point ZHKZDT_RightDown;    //指挥控制大厅显示区域的右下角点
        public Point ZHKZDT_LeftDown;     //指挥控制大厅显示区域的左下角点
        ///席位机区域高度
        public int SeatHeight = 0;
        public int QtHeight = 0;
        //主机房
        public Point QT_LeftUp;            //主机房显示区域的左上角点
        public Point QT_RightUp;           //主机房显示区域的右上角点
        public Point QT_RightDown;         //主机房显示区域的右下角点
        public Point QT_LeftDown;          //主机房显示区域的左下角点

        //集群服务器

        public Point SAN_LeftUp;
        public Point SAN_RightUp;
        public Point SAN_RightDown;
        public Point SAN_LeftDown;

        //固定地面站
        public Point GDDMZ_LeftUp;       //固定地面站显示区域的左上角点
        public Point GDDMZ_RightUp;      //固定地面站显示区域的右上角点
        public Point GDDMZ_RightDown;    //固定地面站显示区域的右下角点
        public Point GDDMZ_LeftDown;     //固定地面站显示区域的左下角点


        //绘图部分的左上点和右下点的坐标 区域的宽和高
        public static int leftupx = 0;
        public static int leftupy = 0;
        public int rewidth = 0;
        public int reheight = 0;

        ////固定地面站 每行图片个数
        public int FixDevNum = 0;
        public double FixDevNumb = 0.0;
        public static int ClickDevID;

        #endregion
        
        public Font MyBigFont = new Font("宋体", 15.0f, GraphicsUnit.Pixel);//定义字体  显示“指挥控制大厅”“主机房”
        public Font MyFont = new Font("宋体", 12.0f, GraphicsUnit.Pixel);//定义字体  显示“指挥控制大厅”“主机房”
        public Font MySmallFont = new Font("宋体", 11.0f, GraphicsUnit.Pixel);//定义字体  显示IP信息的
        public Pen MyPen = new Pen(Color.SteelBlue);                      //定义画笔,绘图时将样式改为点划线,蓝色笔 画指控大厅周围矩形线条的
        public Pen MyQTPen = new Pen(Color.SteelBlue);                    //定义画笔,绘图时将样式改为点划线,蓝色笔 画主机房周围矩形线条的
        public Pen MyNewPen = new Pen(Color.Black);                      //定义画笔,黑色笔
        public Pen GreenPen = new Pen(Color.Green,2);                      //绿色笔
        public Pen RedPen = new Pen(Color.Red, 2);                          //红色笔
        public Pen OlivePen = new Pen(Color.Olive,2);                     //绿色
        public Pen LightGreenPen = new Pen(Color.LightGreen, 2);
        public Pen Arrow = new Pen(Color.SkyBlue,5);  

        private BroadcastSocket bdsock = new BroadcastSocket("", 11000); //本地局域网广播
        public MulticastSocket m_timesock; //接收网络时间的组播套接字

        /*****************************2013-08-21 BYG*************************************/
        public static int width_switch = Topology.image_switchBlue.Width;//交换机图片宽度
        public static int height_switch = Topology.image_switchBlue.Height;//交换机图片高度
        public static int width_seat = Topology.image_HostBlue.Width;//席位机宽度
        public static int height_seat = Topology.image_HostBlue.Height;//席位机高度
        public static int width_server = Topology.image_serverBlue.Width;//服务器宽度
        public static int height_server = Topology.image_serverBlue.Height;//服务器高度
        public static int switch_row = 1;//交换机所在行
        public static int switch_collunm = 1;//交换机所在列
        public static int switch_level = 0;//交换机层数
        public static List<List<Point>> list_lines_seats1 = new List<List<Point>>();//存储席位机连线(一个交换机)
        public static List<List<Point>> list_switches_switches = new List<List<Point>>();//存储交换机连线;
        public static List<List<Point>> list_lines_servers1 = new List<List<Point>>();//存储服务器连线;(一个交换机)
        public static List<List<List<Point>>> list_lines_seats2 = new List<List<List<Point>>>();//存储席位机连线
        public static List<List<List<Point>>> list_lines_servers2 = new List<List<List<Point>>>();//存储服务器连线;
        /********************************************************************************/
        #endregion

        #region 只打开一个窗体实例
        //以下代码是为了只打开一个窗体实例而添加的
        private AlarmCfg _dlgAlarmCfg = null;
        private AlarmCfg AlarmCfgForm
        {
            get { return _dlgAlarmCfg; }
            set { _dlgAlarmCfg = value; }
        }

        private LogCfg _dlgLogCfg = null;
        private LogCfg LogCfgForm
        {
            get { return _dlgLogCfg; }
            set { _dlgLogCfg = value; }
        }

        private DbConnect _dlgDbConnect = null;
        private DbConnect DbConnectForm
        {
            get { return _dlgDbConnect; }
            set { _dlgDbConnect = value;}
        }

      

        private HistroyQuery _dlgHistroyQuery = null;
        private HistroyQuery HistroyQueryForm
        {
            get { return _dlgHistroyQuery; }
            set { _dlgHistroyQuery = value; }
        }

        private LogClear _dlgLogClear = null;
        private LogClear LogClearForm
        {
            get { return _dlgLogClear; }
            set { _dlgLogClear = value; }
        }

        private FolderBrowserDialog _FBDialog = null;
        private FolderBrowserDialog FBDialogForm
        {
            get { return _FBDialog; }
            set { _FBDialog = value;}
        }

        private AlarmInfo _dlgAlarmInfo = null;
        private AlarmInfo AlarmInfoForm
        {
            get { return _dlgAlarmInfo; }
            set { _dlgAlarmInfo = value; }
        }

        public HostInfoForm _dlgHostInfo = null;
        private HostInfoForm hostInfoForm
        {
            get { return _dlgHostInfo; }
            set { _dlgHostInfo = value; }
        }

        public SwitchInfoForm _dlgSwitchInfo = null;
        private SwitchInfoForm switchInfoForm
        {
            get { return _dlgSwitchInfo; }
            set { _dlgSwitchInfo = value; }
        }

        private DataShow _dlgDataShow = null;
        private DataShow DataShowForm
        {
            get { return _dlgDataShow; }
            set { _dlgDataShow = value; }
        }


        private ShutDown _shutdown = null;
        private ShutDown ShutDownForm
        {
            get { return _shutdown; }
            set { _shutdown = value; }
        }

        private Start  _start = null;
        private Start Start
        {
            get { return _start; }
            set { _start = value; }
        }

        private RemoteControl _remote = null;
        private RemoteControl Remote
        {
            get { return _remote; }
            set { _remote = value; }
        }
        #endregion
        public MainForm()
        {
            try 
            {
                InitializeComponent();
                CheckForIllegalCrossThreadCalls = false;

                // reduce flicker
                SetStyle(ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.DoubleBuffer, true);
                this.WindowState = FormWindowState.Maximized;
            }
            catch(Exception ee)
            {
                MessageBoxEx.Show(ee.ToString());
            }
            
        }
        public MainForm(String args)
        {
            try
            {
                InitializeComponent();
                CheckForIllegalCrossThreadCalls = false;

                // reduce flicker
                SetStyle(ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.DoubleBuffer, true);
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        #region  通过权限对主窗体进行初始化
        /// <summary>
        /// 对主窗体初始化.
        /// </summary>
        private void Preen_Main()
        {
            try
            {
                ////读取系统配置文件和日志配置文件
                ReadConfig();
                //读取数据库中的表 客户机和服务器表 初始化hostInfo\serverInfo
                 Topology.setHost();
                 SetforbiddenPros();
               //  Topology.setServer();

                #region 显示界面动态调整
                this.Size = new Size(Screen.GetBounds(this).Width, Screen.GetBounds(this).Height);
                this.tabControlPanel_Menu.Width = Screen.GetBounds(this).Width;
                //#region 左侧树形视图的位置参数
                ////左侧树形视图的位置参数
                this.expandPanel_Left.Width = Screen.GetBounds(this).Width / 6;
                this.expandPanel_Left.Height = Screen.GetBounds(this).Height - this.tabControlPanel_Menu.Height;
                
                SetRegion();
                SetLocation();
                #endregion

                #region 左侧综合信息窗体初始化模块
                this.expandPanel_Left.Size = new Size(this.Size.Width / 6, this.expandPanel_Left.Size.Height);
                SystemInfoInit();
                AlartInfoInit();
                OutputInfoInit();
                #endregion

                #region LED时钟
                SevenSegmentClock clocktime = new SevenSegmentClock();
                clocktime.Parent = this.tabControlPanel_Menu;
                clocktime.Dock = DockStyle.Right;
                clocktime.BackColor = Color.Transparent;
                clocktime.Width = 270;
                clocktime.Height = 73;
                clocktime.ClockColor = Color.Blue;
                clocktime.IsDrawShadow = false;
                clocktime.Start();

                #endregion

                UpdateSystemIfo(); //初始化左侧系统信息窗口内容
                this.Show();
                this.timer1.Start();
                this.mytimer.Start();
                
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region 主窗体加载函数
        private void MainForm_Load(object sender, EventArgs e)
        {
            MessageBoxEx.UseSystemLocalizedString = true;
            try
            {
                this.Hide();
                //20100601 Ding Yiming+ "只开启一个程序"
                CheckOpen();
                Preen_Main();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region 菜单响应函数

        #region 告警参数
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnItem_AlarmPara_Click(object sender, EventArgs e)
        {
            try
            {
                if (AlarmCfgForm == null || AlarmCfgForm.IsDisposed)
                {
                    AlarmCfgForm = new AlarmCfg();
                    AlarmCfgForm.Owner = this;
                }
                AlarmCfgForm.Show();
                AlarmCfgForm.WindowState = FormWindowState.Normal;
                AlarmCfgForm.Activate();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void btnItem_AlarmPara_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "设置告警参数";
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void btnItem_AlarmPara_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "";
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region 日志参数
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnItem_LogPara_Click(object sender, EventArgs e)
        {
            try
            {
                if (LogCfgForm == null || LogCfgForm.IsDisposed)
                {
                    LogCfgForm = new LogCfg();
                    LogCfgForm.Owner = this;
                }
                LogCfgForm.Show();
                LogCfgForm.WindowState = FormWindowState.Normal;
                LogCfgForm.Activate();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void btnItem_LogPara_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "设置日志参数";
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void btnItem_LogPara_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "";
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region 数据库连接
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnItem_DatabasePara_Click(object sender, EventArgs e)
        {
            try 
            {
                if (DbConnectForm == null || DbConnectForm.IsDisposed)
                {
                    DbConnectForm = new DbConnect();
                    DbConnectForm.Owner = this;
                }
                DbConnectForm.Show();
                DbConnectForm.WindowState = FormWindowState.Normal;
                DbConnectForm.Activate();
            }
            catch(Exception ex)
            {
                string str = ex.ToString();
            }
        }

        private void btnItem_DatabasePara_MouseEnter(object sender, EventArgs e)
        {
            try 
            {
                this.label_tipleft.Text = "设置数据库连接";
            }
            catch(Exception ex)
            {
                string str = ex.ToString();
            }
        }

        private void btnItem_DatabasePara_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "";
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
        }
        #endregion

        #region 历史记录查询
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnItem_History_Click(object sender, EventArgs e)
        {
            try
            {
                if (HistroyQueryForm == null || HistroyQueryForm.IsDisposed)
                {
                    HistroyQueryForm = new HistroyQuery();
                    HistroyQueryForm.Owner = this;
                }
                HistroyQueryForm.Show();
                HistroyQueryForm.WindowState = FormWindowState.Normal;
                HistroyQueryForm.Activate();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void btnItem_History_MouseEnter(object sender, EventArgs e)
        {
            try 
            {
                this.label_tipleft.Text = "历史记录查询";
            }
            catch(Exception ex)
            {
                string str = ex.ToString();
            }
        }

        private void btnItem_History_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "";
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
        }
        #endregion

        #region 日志清空
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnItem_LogDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (LogClearForm == null || LogClearForm.IsDisposed)
                {
                    LogClearForm = new LogClear();
                    LogClearForm.Owner = this;
                }
                LogClearForm.Show();
                LogClearForm.WindowState = FormWindowState.Normal;
                LogClearForm.Activate();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void btnItem_LogDelete_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "清空日志";
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
        }

        private void btnItem_LogDelete_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "";
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
        }
        #endregion

        /*远程开机
         */
        private void btn_start_Click(object sender, EventArgs e)
        {
            try 
            {
                if (Start == null || Start.IsDisposed)
                {
                    Start = new Start();
                    Start.Owner = this;
                }
                Start.Show();
                Start.WindowState = FormWindowState.Normal;
                Start.Activate();
            }
            catch(Exception ee)
            {
                MessageBoxEx.Show(ee.ToString());
            }
        }

        private void btn_start_MouseEnter(object sender, EventArgs e)
        {
            this.label_tipleft.Text = "远程开机";
        }

        private void btn_start_MouseLeave(object sender, EventArgs e)
        {
            this.label_tipleft.Text = "";
        }

        /*远程部署
         */
        //private void btn_control_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (Remote == null || Remote.IsDisposed)
        //        {
        //            Remote = new RemoteControl();
        //            Remote.Owner = this;
        //        }
        //        Remote.Show();
        //        Remote.WindowState = FormWindowState.Normal;
        //        Remote.Activate();
        //    }
        //    catch (Exception ee)
        //    {
        //        MessageBoxEx.Show(ee.ToString());
        //    }
        //}


        private void btn_control_MouseEnter(object sender, EventArgs e)
        {
            this.label_tipleft.Text = "远程部署";
        }

        private void btn_control_MouseLeave(object sender, EventArgs e)
        {
            this.label_tipleft.Text = "";
        }

        #region 远程关机
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnItem_ShutDown_Click(object sender, EventArgs e)
        {
            try
            {
                if (ShutDownForm == null || ShutDownForm.IsDisposed)
                {
                    ShutDownForm = new ShutDown();
                    ShutDownForm.Owner = this;
                }
                ShutDownForm.Show();
                ShutDownForm.WindowState = FormWindowState.Normal;
                ShutDownForm.Activate();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void btnItem_ShutDown_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "远程关机";
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
        }

        private void btnItem_ShutDown_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "";
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
        }
        #endregion

        #region 退出系统
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnItem_Exit_Click(object sender, EventArgs e)
        { 
            
          this.Close();  
        }

        private void btnItem_Exit_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "退出系统";
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
        }

        private void btnItem_Exit_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.label_tipleft.Text = "";
            }
            catch (Exception ex)
            {
                string str = ex.ToString();
            }
        }
        #endregion

     
        #endregion

        #region  读取配置文件
        public void ReadConfig()
        {
            MessageBoxEx.UseSystemLocalizedString = true;
            try
            {
                //获取nowPath的值  文件绝对路径
                nowPath = Directory.GetCurrentDirectory();  //2009-5-25+
                nowPath += "\\";                            //2009-5-25+ 

                String path = nowPath + @"Config.xml";

                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(path);
                    XmlElement root =doc.DocumentElement;
                    foreach (XmlNode node in root)
                    {
                        if (node.Name == "ConnectionString")
                        {
                            ConnectionStr = node.InnerText;
                        }
                        else if (node.Name == "GroupAddressOut")
                        {
                            GroupAddress = IPAddress.Parse(node.InnerText);
                        }
                        else if (node.Name == "GroupPortOut")
                        {
                            GroupPort = Convert.ToInt32(node.InnerText);
                        }
                        else if (node.Name == "GroupAddressInner")
                        {
                            GroupAddress2 = IPAddress.Parse(node.InnerText);
                        }
                        else if (node.Name == "GroupPortInner")
                        {
                            GroupPort2 = Convert.ToInt32(node.InnerText);
                        }
                        else if (node.Name == "CPU")
                        {
                            CPUVALUE = Convert.ToInt32(node.InnerText);
                        }
                        else if (node.Name == "Disk") 
                        {
                            DISKVALUE = Convert.ToInt32(node.InnerText);
                        }
                        else if (node.Name == "Memory")
                        {
                            MEMVALUE = Convert.ToInt32(node.InnerText);
                        }
                        else if (node.Name == "Traffic")
                        {
                            TRAFFICVALUE = Convert.ToInt32(node.InnerText);
                        }
                        //else if (node.Name == "LocalIp")
                        //{
                        //    strLocalAddrIP =node.InnerText;
                        //}
                        else if (node.Name == "Multicasrip")
                        {
                            Multicasrip = IPAddress.Parse(node.InnerText);
                        }
                        else if (node.Name == "Multicastport")
                        {
                            Multicastport = Convert.ToInt32(node.InnerText);
                        }
                        else if (node.Name == "Loginterval")
                        {
                            loginterval = Convert.ToInt32(node.InnerText);
                        }
                        else if (node.Name == "LogSavetime")
                        {
                            m_savetime = node.InnerText;
                        }
                        else if (node.Name == "Community")
                        {
                            Community = node.InnerText;
                        }
                        else if (node.Name == "Statinterval")
                        {
                            statInterval = Convert.ToInt32(node.InnerText);
                        }
                        else if (node.Name == "LogPath")
                        {
                            if (node.InnerText != "")
                            {
                                try
                                {
                                    if (!Directory.Exists(node.InnerText)) ;
                                    {
                                        Directory.CreateDirectory(node.InnerText);
                                    }
                                    m_LogPath = node.InnerText;
                                }
                                catch (Exception ex)
                                {
                                    if (!Directory.Exists(string.Format("{0}LogFile", nowPath))) ;
                                    {
                                        Directory.CreateDirectory(nowPath + "LogFile");
                                    }
                                    m_LogPath = nowPath;
                                    node.InnerText = m_LogPath;
                                }
                            }
                            else
                            {
                                Directory.CreateDirectory(nowPath + "LogFile");
                                m_LogPath = nowPath;
                                node.InnerText = m_LogPath;
                            }
                        }
                    }
                    strLocalAddrIP = GetLocalIp();
                    doc.Save(path);
                }
                catch (Exception ex)
                {
                    MessageBoxEx.Show("指定的参数配置文件不存在或出现错误，请检查！" + Environment.NewLine +
                        "异常:" + ex.Message);
                }
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private string GetLocalIp()
        {
            string hostname = Dns.GetHostName();
            string ipaddr = "";
            IPHostEntry localhost = Dns.GetHostEntry(hostname);
            string pattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            ////判断系统是否是windows7
            //if ((Environment.OSVersion.Platform == PlatformID.Win32NT) && (Environment.OSVersion.Version.Major >= 6) && (Environment.OSVersion.Version.Minor >=1))
            //{
            //    IPAddress localaddr = localhost.AddressList[1];
            //    ipaddr = localaddr.ToString();
            //}
            //else
            //{
            //    IPAddress localaddr = localhost.AddressList[0];
            //    ipaddr = localaddr.ToString();
            //}
            for (int i = 0; i < localhost.AddressList.Length; i++)
            {
                IPAddress localaddr = localhost.AddressList[i];
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                MatchCollection matches = rgx.Matches(localaddr.ToString());
                if (matches.Count > 0)
                {
                    ipaddr = matches[0].Value;
                    break;
                }
            }
            return ipaddr;
        }
        #endregion

        #region 实现Ping的功能函数
        public bool Ping(String ip)
        {
            int nLoop;
            for (nLoop = 0; nLoop < 3; nLoop++)
            {
                if (PingResult(ip))
                {
                    for (int i = 0; i < allInfo.Count; i++)
                    {
                        if (allInfo[i].m_sIPInDatabase == ip)
                        {
                            allInfo[i].bActive = true;
                            //allInfo[i].bNormal = true;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        //ping远程主机的代码 返回Success\TimedOut\,能Ping通返回true,否则返回false
        public static bool PingResult(String ip)
        {
            System.Net.NetworkInformation.Ping p = new System.Net.NetworkInformation.Ping();
            System.Net.NetworkInformation.PingOptions options = new System.Net.NetworkInformation.PingOptions();
            options.DontFragment = true;
            String data = "aa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            //Wait seconds for a reply.
            int timeout = 1000;

            try
            {
                System.Net.NetworkInformation.PingReply reply = p.Send(ip, timeout, buffer, options);
                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                String str = e.ToString();
                return false;
            }
            return false;
        }
        #endregion

        #region 检查监控程序是否已经打开
        /// <summary>
        /// 只运行一个程序
        /// </summary>
        private void CheckOpen()
        {
            const string mutexName = "系统监控";

            // Attempt to open the named mutex.
            try
            {
                mutex = Mutex.OpenExisting(mutexName);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                doesNotExist = true;
            }
            catch
            {
                return;
            }

            if (doesNotExist)
            {
                mutex = new Mutex(true, mutexName);
            }
            else
            {
                MessageBoxEx.UseSystemLocalizedString = true;
                MessageBoxEx.Show("信息监控与参数设置软件已经打开！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
        }
        #endregion

        #region 初始化左侧综合信息窗口

        //初始化左侧系统信息窗口
        private void SystemInfoInit()
        {
            try
            {
                ListViewItem newItem = new ListViewItem("系统工作模式");
                newItem.SubItems.Add("技术状态检测模式");
                this.listViewSysInfo.Items.Add(newItem);
                newItem = new ListViewItem("用户名");
                this.listViewSysInfo.Items.Add(newItem);

            }
            catch (Exception ex)
            {
                MessageBoxEx.UseSystemLocalizedString = true;
                MessageBoxEx.Show(ex.ToString());
            }
        }
        //初始化告警信息窗口
        private void AlartInfoInit()
        {
            try
            {
                string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                ListViewItem newItem = new ListViewItem("软件系统状态为正常运行");
                newItem.SubItems.Add(timeStr);
                this.listViewAlarmInfo.ContextMenuStrip = contextMenuStrip1;
                this.listViewAlarmInfo.Items.Add(newItem);
                

            }
            catch (Exception ex)
            {
                MessageBoxEx.UseSystemLocalizedString = true;
                MessageBoxEx.Show(ex.ToString());
            }

        }
        //初始化输出信息窗口
        private void OutputInfoInit()
        {
            try
            {
                string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                ListViewItem newItem = new ListViewItem("系统控制：设置工作模式为技术检测模式");
                newItem.SubItems.Add(timeStr);
                MainForm.listViewOutputInfo.Items.Add(newItem);

            }
            catch (Exception ex)
            {
                MessageBoxEx.UseSystemLocalizedString = true;
                MessageBoxEx.Show(ex.ToString());
            }

        }
        #endregion

        #region 控制左侧listview项的追加和跟新

        //追加一项到listview
        public static void AddListViewItem(ListViewEx mylistview, string col1, string col2, Color bkcolor)
        {
            try
            {
                if (mylistview.InvokeRequired)
                {
                    MyDelegate md = new MyDelegate(AddListViewItem);
                    mylistview.Invoke(md,new object[] {mylistview,col1,col2,bkcolor});
                }
                else
                {
                    ListViewItem newItem = new ListViewItem(col1);
                    newItem.SubItems.Add(col2);
                    newItem.BackColor = bkcolor;
                    mylistview.Items.Add(newItem);
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.UseSystemLocalizedString = true;
                MessageBoxEx.Show(ex.ToString());
            }
        }

        public static void AddListViewItem(ListViewEx mylistview, string col1, string col2, string col3, Color bkcolor)
        {
            try
            {
                if (mylistview.InvokeRequired)
                {
                    MyDelegate2 md = new MyDelegate2(AddListViewItem);
                    mylistview.Invoke(md, new object[] { mylistview, col1, col2, col3, bkcolor });
                }
                else
                {
                    ListViewItem newItem = new ListViewItem(col1);
                    newItem.SubItems.Add(col2);
                    newItem.SubItems.Add(col3);
                    newItem.BackColor = bkcolor;
                    mylistview.Items.Add(newItem);
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.UseSystemLocalizedString = true;
                MessageBoxEx.Show(ex.ToString());
            }
        }

        //更新列表某一项,如果没有该项则追加到结尾
        public static void UpdateListViewItem(ListViewEx mylistview, string col1, string col2, Color bkcolor)
        {
            try
            {
                if (mylistview.InvokeRequired)
                {
                    MyDelegate md = new MyDelegate(UpdateListViewItem);
                    mylistview.Invoke(md, new object[] { mylistview, col1, col2, bkcolor });
                }
                else
                {
                    ListViewItem LvItem = mylistview.FindItemWithText(col1);
                    if (LvItem != null)
                    {
                        int index = LvItem.Index;
                        LvItem.SubItems.Clear();
                        LvItem = new ListViewItem(col1);
                        LvItem.SubItems.Add(col2);
                        LvItem.BackColor = bkcolor;
                        mylistview.Items.Insert(index, LvItem);
                        mylistview.Items.RemoveAt(index + 1);
                    }
                    else
                    {
                        LvItem = new ListViewItem(col1);
                        LvItem.SubItems.Add(col2);
                        LvItem.BackColor = bkcolor;
                        mylistview.Items.Add(LvItem);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.UseSystemLocalizedString = true;
                MessageBoxEx.Show(ex.ToString());
            }

        }

        #endregion

        #region 更新左侧的系统监控信息
        private void UpdateSystemIfo()
        {
            try
            {
                for (int k = 0; k < serverInfo.Count; k++)
                {
                    if (serverInfo[k].bActive == true && serverInfo[k].lCurRecvLen == 0)
                    {
                        UpdateListViewItem(this.listViewSysInfo, "服务器(" + serverInfo[k].m_sIPInDatabase + ")", "已开机", System.Drawing.Color.White);
                    }
                    else if (serverInfo[k].bActive == true && serverInfo[k].lCurRecvLen != 0)
                    {
                        if (serverInfo[k].bNormal == true)
                        {
                            UpdateListViewItem(this.listViewSysInfo, "服务器(" + serverInfo[k].hostName + ")", "有数据流通", System.Drawing.Color.White);
                        }
                        else if (serverInfo[k].bNormal == false)
                        {
                            UpdateListViewItem(this.listViewSysInfo, "服务器(" + serverInfo[k].hostName + ")", "有异常", System.Drawing.Color.White);
                        }
                    }
                    else if (serverInfo[k].bActive == false)
                    {
                        UpdateListViewItem(this.listViewSysInfo, "服务器(" + serverInfo[k].hostName + ")", "关机中", System.Drawing.Color.White);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.UseSystemLocalizedString = true;
                MessageBoxEx.Show(ex.ToString());
            }
        }

        public static void mfUpdataOutput(string outInfo)
        {
            MessageBoxEx.UseSystemLocalizedString = true;
            try
            {
                string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                AddListViewItem(MainForm.listViewOutputInfo, outInfo, timeStr, System.Drawing.Color.LightGreen);
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(ex.ToString());
            }
        }
        #endregion

        #region 显示大小变化时重绘
        /*左侧列表收缩或者展开时  重画页面2011-08-16
         */   
        private void expandPanel_Left_ExpandedChanged(object sender, ExpandedChangeEventArgs e)
        {
            try 
            {
                listClear();
                SetRegion();
                //首先设置交换机位置
                SetLocation();
                this.panelEx1.Refresh();
            }
            catch(Exception ex)
            {
                MessageBoxEx.Show(ex.ToString());
            }
        }
        
        #endregion

        #region 设置拓扑图显示区域
        /// <summary>
        /// 设置整个绘图区域位置
        /// </summary>
        private void SetRegion()
        {
            try
            {
                //拓扑图部分显示区域的位置参数
                rewidth = Screen.GetBounds(this).Width - this.expandPanel_Left.Width ;
                reheight = Screen.GetBounds(this).Height - 130-50; //- this.panel3.Height - this.panel_data.Height - this.panel_video.Height - this.tableLayoutPanel3.Height
                leftupx = 1;
                leftupy = 20;  
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        #endregion

        #region 计时器 启动界面后再开始整个ping机器及初始化拓扑的过程,拓扑完成后再读取数据库和启动线程
        private void mytimer_Tick(object sender, EventArgs e)
        {
            try
            {
                init_topo = new Thread(new ThreadStart(this.InitTopoStart));
                init_topo.Start();
                this.mytimer.Stop();

                DeleteRedundantLog();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void InitTopoStart()
        {
            try
            {
                RecvFromClientRun();
                InitPing();   //运行比较慢              
                PingHostRun();
                init_topo.Abort();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        #endregion

        #region 开始ping各个IP,拓扑过程
        //ping各个IP 初始化m_sIPActiveDevices,以及拓扑过程
        private void InitPing()
        {
            try
            {
                for (int i = 0; i < serverInfo.Count; i++)
                {
                    if (PingResult(serverInfo[i].m_sIPInDatabase))
                    {
                        serverInfo[i].bActive = true;
                        serverInfo[i].bNormal = true;
                    }
                }
                for (int i = 0; i < hostInfo.Count; i++)
                {
                    if (PingResult(hostInfo[i].m_sIPInDatabase))
                    {
                        hostInfo[i].bActive = true;
                        hostInfo[i].bNormal = true;
                    }
                }
                this.panelEx1.Refresh();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region 从客户端代理接收信息的线程,分析收到的消息
        //创建从代理接收信息的线程,并以"RecvFromClient"过程来初始化线程实例
        private void RecvFromClientRun()
        {
            try
            {
                server = new UdpClient(GroupPort);        //注意:一定要用组播端口创建UdpClient,否则收不到组播数据！
                groupEP = new IPEndPoint(GroupAddress, GroupPort);
                //server.JoinMulticastGroup(GroupAddress);
                server.JoinMulticastGroup(GroupAddress, IPAddress.Parse(strLocalAddrIP)); //20090603gai
                server.Ttl = 16;
                //server.Connect(groupEP);               //注意:在调用Receive之前不要调用Connect,否则不能正常接收组播数据
                //开一个线程
                recvInfoFromClient = new Thread(new ThreadStart(this.RecvFromClient));
                //启动线程
                recvInfoFromClient.Start();
            }
            catch (Exception e)
            {
                String str = e.ToString();
            }

        }

        //接收客户端代理发到组播地址"239.255.0.11"和端口"5150"上的数据
        private void RecvFromClient()
        {
            try
            {
                while (true)
                {
                    byte[] bytes = server.Receive(ref groupEP);
                    String recvStr = Encoding.Default.GetString(bytes); //--ding 20090603gai
                    AnalyzeRecvInfo(recvStr, recvStr.Length,groupEP.Address.ToString());
                }
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        //分析收到的消息
        public void AnalyzeRecvInfo(String recvbuf, long recvLen, string ipaddr)
        {
            try
            {
                String recvdata = recvbuf;         //从监控代理接收到的数据

                int position;

                if (!recvdata.StartsWith("$"))      //收到的数据中包含服务器双工状态信息

                {
                    //将相应双机热备的信息除去后 赋给recvdata ,数据的格式应为 $计算机名$CPU...
                    String statustr = recvdata.Substring(0, recvdata.IndexOf('$'));  //得到的双机热备数据
                    recvdata = recvdata.Substring(recvdata.IndexOf('$'));    //将相应双机热备的信息除去后 赋给recvdata
                }
                string macpos = recvdata.Substring(recvdata.LastIndexOf('$')+1,12);
                recvdata = recvdata.Substring(0, recvdata.LastIndexOf('$'));
                recvdata = recvdata.Substring(1);    //去掉最前面的$
                position = recvdata.IndexOf('$');
                String str;
                if (position > 0)
                {
                    str = recvdata.Substring(0, position);
                }
                else
                {
                    str = "";
                }


                #region 循环判断是哪个HOST
                for (int i = 0; i < hostInfo.Count; i++)
                {
                    if (ipaddr == hostInfo[i].m_sIPInDatabase)
                    {
                        String sInfo = recvdata;
                        hostInfo[i].sInfo = sInfo;
                        hostInfo[i].lPreRecvLen = hostInfo[i].lCurRecvLen;
                        hostInfo[i].lCurRecvLen += recvLen;

                        bool flag = hostInfo[i].bNormal;  //之前的状态
                        bool flag1 = true;  //CPU
                        bool flag2 = true;  //Disk
                        bool flag3 = true;  //Memory
                        bool flag4 = true; //Traffic
                        bool flag5 = true; //Forbidden Process
                        bool flag6 = true; //Forbidden Service
                        sInfo = sInfo.Substring(sInfo.IndexOf('$') + 1);     //将接收到的数据字符串去掉最前面的主机名

                        int nValue;
                        LogInfo logInfo = new LogInfo();
                        logInfo.hostid = hostInfo[i].clientID;
                        logInfo.ipaddr = hostInfo[i].m_sIPInDatabase;
                        logInfo.hostname = hostInfo[i].hostName;

                        //CPU使用率的获取
                        position = sInfo.IndexOf('$');
                        if (position > 0)
                        {
                            nValue = Convert.ToInt32(sInfo.Substring(0, position));
                        }
                        else
                        {
                            nValue = 0;
                        }

                        if (nValue > hostInfo[i].cpuTH && hostInfo[i].cpuValue < hostInfo[i].cpuTH) //allInfo[i].cpuValue < MainForm.allInfo[i].cpuTH && //20100422 Ding Yiming注释
                        {
                            logInfo.logcontent = "CPU使用率超过门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            flag1 = false;
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(),System.Drawing.Color.Yellow);
                        }
                        else if (nValue < hostInfo[i].cpuTH && hostInfo[i].cpuValue > hostInfo[i].cpuTH) //if (allInfo[i].cpuValue > MainForm.allInfo[i].cpuTH && nValue < MainForm.allInfo[i].cpuTH)//20100422 Ding Yiming注释
                        {
                                
                            flag1 = true;
                            logInfo.logcontent = "CPU使用率恢复正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(),System.Drawing.Color.Green);      

                        }
                        else if (nValue > hostInfo[i].cpuTH && hostInfo[i].cpuValue > hostInfo[i].cpuTH)
                        {
                            flag1 = false;
                            logInfo.logcontent = "CPU使用率超过门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            flag1 = false;
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Yellow);
                        }
                        else
                        {
                            flag1 = true; 
                        }
                        hostInfo[i].cpuValue = nValue;
                        sInfo = sInfo.Substring(sInfo.IndexOf('$') + 1);     //将接收到的数据字符串再去掉 CPU使用率

                        //硬盘空间的获取
                        nValue = Convert.ToInt32(sInfo.Substring(0, sInfo.IndexOf('/')));
                        if (nValue > 0 && nValue < hostInfo[i].diskTH * 1024 && hostInfo[i].diskValue > hostInfo[i].diskTH * 1024)//allInfo[i].diskValue > MainForm.allInfo[i].diskTH && //20100422 Ding Yiming注释
                        {
                            flag2 = false;
                            logInfo.logcontent = "硬盘剩余容量低于门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();                           
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr,i.ToString(), System.Drawing.Color.Yellow);
                        }
                        else if (nValue > hostInfo[i].diskTH * 1024 && hostInfo[i].diskValue < hostInfo[i].diskTH * 1024)//if (allInfo[i].diskValue < MainForm.allInfo[i].diskTH && nValue > MainForm.allInfo[i].diskTH)//20100422 Ding Yiming注释
                        {
                            flag2 = true;
                            logInfo.logcontent = "硬盘剩余容量正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(),System.Drawing.Color.Green);                        
                        }
                        else if (nValue > 0 && nValue < hostInfo[i].diskTH * 1024 && hostInfo[i].diskValue < hostInfo[i].diskTH * 1024)
                        {
                            flag2 = false;
                            logInfo.logcontent = "硬盘剩余容量低于门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Yellow);
                        }
                        else
                        {
                            flag2 = true;
                        }
                        hostInfo[i].diskValue = nValue;
                        sInfo = sInfo.Substring(sInfo.IndexOf('/') + 1);     //将接收到的数据字符串再去掉 可用硬盘空间
                        sInfo = sInfo.Substring(sInfo.IndexOf('$') + 1);     //将接收到的数据字符串再去掉 硬盘总空间

                        //内存大小的获取
                        nValue = Convert.ToInt32(sInfo.Substring(0, sInfo.IndexOf('/')));
                        if (nValue < hostInfo[i].memTH && hostInfo[i].memValue > hostInfo[i].memTH)//allInfo[i].memValue > MainForm.allInfo[i].memTH && //20100422 Ding Yiming注释
                        {  
                            flag3 = false;
                            logInfo.logcontent = "内存剩余量低于门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();                        
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr,i.ToString(), System.Drawing.Color.Yellow);

                        }
                        else if (nValue > hostInfo[i].memTH && hostInfo[i].memValue < hostInfo[i].memTH)//if (allInfo[i].memValue < MainForm.allInfo[i].memTH && nValue > MainForm.allInfo[i].memTH)//20100422 Ding Yiming注释
                        {
                            flag3 = true;
                            logInfo.logcontent = "内存剩余量正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(),System.Drawing.Color.Green);
                         
                        }
                        else if (nValue < hostInfo[i].memTH && hostInfo[i].memValue < hostInfo[i].memTH)
                        {
                            flag3 = false;
                            logInfo.logcontent = "内存剩余量低于门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr,i.ToString(), System.Drawing.Color.Yellow);
                        }
                        else
                        {
                            flag3 = true;
                        }
                        hostInfo[i].memValue = nValue;
                        sInfo = sInfo.Substring(sInfo.IndexOf('/') + 1);     //将接收到的数据字符串再去掉 可用内存
                        sInfo = sInfo.Substring(sInfo.IndexOf('$') + 1);     //将接收到的数据字符串再去掉 内存总大小


                        while (!sInfo.StartsWith("$"))       //提取进程信息
                        {
                            String strPro;
                            strPro = sInfo.Substring(0, sInfo.IndexOf('&'));

                            int pos;
                            pos = strPro.IndexOf('#');   //进程名称#进程具体信息 查找该#
                            bool iter =false;
                            if (hostInfo[i].mProcInfo != null)
                            {
                                iter = hostInfo[i].mProcInfo.ContainsKey(strPro.Substring(0, pos));
                            }
                           
                            if (iter)               //有该进程信息
                            {
                                //开始执行现在不执行
                                if (strPro.Substring(pos + 1) == "NOT EXECUTED"&& hostInfo[i].mProcInfo[strPro.Substring(0, pos)] == true)                                    
                                {
                                    if (hostInfo[i].forbiddenPro.ContainsKey(strPro.Substring(0, pos)))
                                    {
                                        flag5 = true;
                                        logInfo.logcontent = "被禁用进程" + strPro.Substring(0, pos) + "停止执行";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    else
                                    {                                       
                                        logInfo.logcontent = "进程" + strPro.Substring(0, pos) + "停止执行";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    hostInfo[i].mProcInfo[strPro.Substring(0, pos)] = false;  //设置该进程对应的状态为false
                                }
                                //开始不执行现在执行
                                else if (strPro.Substring(pos + 1) != "NOT EXECUTED"&& hostInfo[i].mProcInfo[strPro.Substring(0, pos)] == false)                                   
                                {
                                    if (hostInfo[i].forbiddenPro.ContainsKey(strPro.Substring(0, pos)))
                                    {
                                        flag5 = false;
                                        logInfo.logcontent = "被禁用进程" + strPro.Substring(0, pos) + "开始启动";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Red);
                                    }
                                    else
                                    {                                                       
                                        logInfo.logcontent = "进程" + strPro.Substring(0, pos) + "开始执行";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    hostInfo[i].mProcInfo[strPro.Substring(0, pos)] = true; //设置该进程对应的状态为true                    
                                }
                                else if (strPro.Substring(pos + 1) != "NOT EXECUTED" && hostInfo[i].mProcInfo[strPro.Substring(0, pos)] == true)
                                {
                                    if (hostInfo[i].forbiddenPro.ContainsKey(strPro.Substring(0, pos)))
                                    {
                                        flag5 = false;
                                    }
                                }
                            }                           
                            else             //没有该进程信息
                            {
                                if (strPro.Substring(pos + 1) == "NOT EXECUTED")
                                {
                                    hostInfo[i].mProcInfo.Add(strPro.Substring(0, pos), false);
                                    logInfo.logcontent = "进程" + strPro.Substring(0, pos) + "未执行";
                                    logInfo.maintype = "操作";
                                    logInfo.senctype = "设备";
                                    logInfo.WriteLog();
                                    string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                    AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Yellow);
                                }
                                else
                                {
                                    hostInfo[i].mProcInfo.Add(strPro.Substring(0, pos), true);
                                    if (hostInfo[i].forbiddenPro.ContainsKey(strPro.Substring(0, pos)))
                                    {
                                        flag5 = false;
                                        logInfo.logcontent = "被禁用进程" + strPro.Substring(0, pos) + "已启动";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Red);
                                    }
                                }
                            }
                            sInfo = sInfo.Substring(sInfo.IndexOf('&') + 1);
                        }
                        sInfo = sInfo.Substring(1);

                        while (!sInfo.StartsWith("$"))       //提取服务信息
                        {
                            String strSer;
                            strSer = sInfo.Substring(0, sInfo.IndexOf('&'));

                            int pos;
                            pos = strSer.IndexOf('#');   //服务名称#服务状态 查找该#
                            bool iter;
                            iter = hostInfo[i].mServInfo.ContainsKey(strSer.Substring(0, pos));
                            if (iter)               //有该服务信息
                            {
                                if (strSer.Substring(pos + 1) == "4"&& hostInfo[i].mServInfo[strSer.Substring(0, pos)] == false)                                 
                                {
                                    if (hostInfo[i].forbiddenPro.ContainsKey(strSer.Substring(0, pos)))
                                    {
                                        flag6 = false;
                                        logInfo.logcontent = "被禁用服务" + strSer.Substring(0, pos) + "已启动";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Red);
                                    }
                                    //设置该服务对应的状态为true
                                    else
                                    {
                                        logInfo.logcontent = "服务" + strSer.Substring(0, pos) + "开始运行";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Green);
                                    }                                    
                                    hostInfo[i].mServInfo[strSer.Substring(0, pos)] = true;
                                }
                                else if (strSer.Substring(pos + 1) == "1"&& hostInfo[i].mServInfo[strSer.Substring(0, pos)] == true)                         
                                {
                                    if (hostInfo[i].forbiddenPro.ContainsKey(strSer.Substring(0, pos)))
                                    {
                                        flag6 = true;
                                        logInfo.logcontent = "被禁用服务" + strSer.Substring(0, pos) + "停止";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    else
                                    {
                                        logInfo.logcontent = "服务" + strSer.Substring(0, pos) + "停止";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Red);
                                    }
                                    hostInfo[i].mServInfo[strSer.Substring(0, pos)] = false;//设置该服务对应的状态为false
                                }
                                else if (strSer.Substring(pos + 1) == "4" && hostInfo[i].mServInfo[strSer.Substring(0, pos)] == true)
                                {
                                    if (hostInfo[i].forbiddenPro.ContainsKey(strSer.Substring(0, pos)))
                                           flag6 = false;
                                }
                            }                         
                            else                   //没有该服务信息
                            {
                                if (strSer.Substring(pos + 1) == "1")
                                {
                                    hostInfo[i].mServInfo.Add(strSer.Substring(0, pos), false);
                                    logInfo.logcontent = "服务" + strSer.Substring(0, pos) + "未运行";
                                    logInfo.maintype = "操作";
                                    logInfo.senctype = "设备";
                                    logInfo.WriteLog();
                                    string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                    AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(),System.Drawing.Color.Yellow);
                                }
                                else if (strSer.Substring(pos + 1) == "4")
                                {
                                    hostInfo[i].mServInfo.Add(strSer.Substring(0, pos), true);
                                    if (hostInfo[i].forbiddenPro.ContainsKey(strSer.Substring(0, pos)))
                                    {
                                        flag6 = false;
                                        logInfo.logcontent = "被禁用服务" + strSer.Substring(0, pos) + "已启动";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Red);
                                    }
                                }
                            }
                            sInfo = sInfo.Substring(sInfo.IndexOf('&') + 1);
                        }

                        //提取流量信息      $接口#带宽#输入流量#输出流量#总流量#
                        String strTraffic;
                        strTraffic = recvdata;// allInfo[i].sInfo;
                        strTraffic = strTraffic.Substring(strTraffic.LastIndexOf('$') + 1);    //提取 接口#带宽#输入流量#输出流量#总流量#

                        hostInfo[i].netPortName = strTraffic.Substring(0, strTraffic.IndexOf('#'));    //提取 接口
                        strTraffic = strTraffic.Substring(strTraffic.IndexOf('#') + 1);                           //去掉 接口#

                        hostInfo[i].bandWidth = Convert.ToInt32(strTraffic.Substring(0, strTraffic.IndexOf('#')));    //提取 带宽
                        strTraffic = strTraffic.Substring(strTraffic.IndexOf('#') + 1);                                        //去掉 带宽#

                        hostInfo[i].inComingTraffic = Convert.ToDouble(strTraffic.Substring(0, strTraffic.IndexOf('#')));  //提取 输入流量
                        strTraffic = strTraffic.Substring(strTraffic.IndexOf('#') + 1);                                             //去掉 输入流量#

                        hostInfo[i].outGoingTraffic = Convert.ToDouble(strTraffic.Substring(0, strTraffic.IndexOf('#')));  //提取 输出流量
                        strTraffic = strTraffic.Substring(strTraffic.IndexOf('#') + 1);                                             //去掉 输出流量#

                        double tValue = Convert.ToDouble(strTraffic.Substring(0, strTraffic.IndexOf('#')));
                        //流量预警
                        if (tValue * 8.0 > hostInfo[i].trafficTH * 1024 && hostInfo[i].totalTraffic * 8.0 < hostInfo[i].trafficTH * 1024)
                        {
                            logInfo.logcontent = "流量超过门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "主机";
                            logInfo.WriteLog();
                            flag4 = false;
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Yellow);
                        }
                        else if (tValue * 8.0 > hostInfo[i].trafficTH * 1024 && hostInfo[i].totalTraffic * 8.0 > hostInfo[i].trafficTH * 1024)
                        {
                            flag4 = false;
                        }
                        else if (tValue * 8.0 < hostInfo[i].trafficTH * 1024 && hostInfo[i].totalTraffic * 8.0 > hostInfo[i].trafficTH * 1024)
                        {
                            flag4 = true;
                            logInfo.logcontent = "流量恢复正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "主机";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Green);
                        }
                        else
                        {
                            flag4 = true;
                        }               
                        hostInfo[i].totalTraffic = Convert.ToDouble(strTraffic.Substring(0, strTraffic.IndexOf('#')));    //提取 总流量
                        //修改超过预警值的状态，及恢复时候的状态
                        if ((flag1 && flag2 && flag3 && flag4 &&flag5 && flag6) == false)
                        {
                            hostInfo[i].bNormal = false;
                            if (IsVoiceOn)
                                Console.Beep();
                        }
                        else
                            hostInfo[i].bNormal = true;

                        goto End;
                    }

                }
                #endregion

                #region 循环判断是哪个SERVER
                for (int i = 0; i < serverInfo.Count; i++)
                {
                    if (ipaddr == serverInfo[i].m_sIPInDatabase)
                    {
                        String sInfo = recvdata;
                        serverInfo[i].sInfo = sInfo;
                        serverInfo[i].lPreRecvLen = serverInfo[i].lCurRecvLen;
                        serverInfo[i].lCurRecvLen += recvLen;
                        int allid = i + hostInfo.Count;
                        bool flag = serverInfo[i].bNormal;  //之前的状态
                        bool flag1 = true;  //CPU
                        bool flag2 = true;  //Disk
                        bool flag3 = true;  //Memory
                        bool flag4 = true; //Traffic
                        bool flag5 = true; //Forbidden Process
                        bool flag6 = true; //Forbidden Service
                        sInfo = sInfo.Substring(sInfo.IndexOf('$') + 1);     //将接收到的数据字符串去掉最前面的主机名

                        int nValue;
                        LogInfo logInfo = new LogInfo();
                        logInfo.hostid = serverInfo[i].clientID;
                        logInfo.ipaddr = serverInfo[i].m_sIPInDatabase;
                        logInfo.hostname = serverInfo[i].hostName;

                        //CPU使用率的获取
                        position = sInfo.IndexOf('$');
                        if (position > 0)
                        {
                            nValue = Convert.ToInt32(sInfo.Substring(0, position));
                        }
                        else
                        {
                            nValue = 0;
                        }

                        if (nValue > serverInfo[i].cpuTH && serverInfo[i].cpuValue < serverInfo[i].cpuTH)  //allInfo[i].cpuValue < MainForm.allInfo[i].cpuTH && //20100422 Ding Yiming注释
                        {
                            logInfo.logcontent = "CPU使用率超过门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            flag1 = false;
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(),System.Drawing.Color.Yellow);
                        }
                        else if (nValue < serverInfo[i].cpuTH && serverInfo[i].cpuValue > serverInfo[i].cpuTH)//if (allInfo[i].cpuValue > MainForm.allInfo[i].cpuTH && nValue < MainForm.allInfo[i].cpuTH)//20100422 Ding Yiming注释
                        {
                                logInfo.logcontent = "CPU使用率正常";
                                logInfo.maintype = "操作";
                                logInfo.senctype = "设备";
                                logInfo.WriteLog();
                                flag1 = false;
                                string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(),System.Drawing.Color.Green);                        

                        }
                        else if (nValue > serverInfo[i].cpuTH && serverInfo[i].cpuValue > serverInfo[i].cpuTH)
                        {
                            flag1 = false;
                            logInfo.logcontent = "CPU使用率超过门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                       //     AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(), System.Drawing.Color.Yellow);
                        }
                        else
                        {
                            flag1 = true;
                        }
                        serverInfo[i].cpuValue = nValue;
                        sInfo = sInfo.Substring(sInfo.IndexOf('$') + 1);     //将接收到的数据字符串再去掉 CPU使用率

                        //硬盘空间的获取
                        nValue = Convert.ToInt32(sInfo.Substring(0, sInfo.IndexOf('/')));
                        if (nValue > 0 && nValue < serverInfo[i].diskTH * 1024 && serverInfo[i].diskValue > serverInfo[i].diskTH * 1024) //allInfo[i].diskValue > MainForm.allInfo[i].diskTH && //20100422 Ding Yiming注释
                        {
                            logInfo.logcontent = "硬盘剩余容量低于门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            flag2 = false;
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(), System.Drawing.Color.Yellow);
                        }
                        else if (nValue > serverInfo[i].diskTH * 1024 && serverInfo[i].diskValue < serverInfo[i].diskTH * 1024) //if (allInfo[i].diskValue < MainForm.allInfo[i].diskTH && nValue > MainForm.allInfo[i].diskTH)//20100422 Ding Yiming注释
                        {
                            flag2 = true;
                            logInfo.logcontent = "硬盘剩余容量正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(), System.Drawing.Color.Green);                      
                        }
                        else if (nValue > 0 && nValue < serverInfo[i].diskTH * 1024 && serverInfo[i].diskValue < serverInfo[i].diskTH * 1024)
                        {
                            flag2 = false;
                            logInfo.logcontent = "硬盘剩余容量低于门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(), System.Drawing.Color.Yellow);
                        }
                        else
                        {
                            flag2 = true;
                        }
                        serverInfo[i].diskValue = nValue;
                        sInfo = sInfo.Substring(sInfo.IndexOf('/') + 1);     //将接收到的数据字符串再去掉 可用硬盘空间
                        sInfo = sInfo.Substring(sInfo.IndexOf('$') + 1);     //将接收到的数据字符串再去掉 硬盘总空间

                        //内存大小的获取
                        nValue = Convert.ToInt32(sInfo.Substring(0, sInfo.IndexOf('/')));
                        if (nValue < serverInfo[i].memTH && serverInfo[i].memValue > serverInfo[i].memTH)//allInfo[i].memValue > MainForm.allInfo[i].memTH && //20100422 Ding Yiming注释
                        {
                            logInfo.logcontent = "内存剩余量低于门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            flag3 = false;
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(), System.Drawing.Color.Yellow);

                        }
                        else if (nValue > serverInfo[i].memTH && serverInfo[i].memValue < serverInfo[i].memTH)  //if (allInfo[i].memValue < MainForm.allInfo[i].memTH && nValue > MainForm.allInfo[i].memTH)//20100422 Ding Yiming注释
                        {
                            flag3 = true;
                            logInfo.logcontent = "内存剩余量正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(), System.Drawing.Color.Green);
                         
                        }
                        else if (nValue < serverInfo[i].memValue && serverInfo[i].memValue < serverInfo[i].memTH)
                        {
                            flag3 = false;
                            logInfo.logcontent = "内存剩余量低于门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "设备";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(), System.Drawing.Color.Yellow);
                        }
                        else
                        {
                            flag3 = true;
                        }
                        serverInfo[i].memValue = nValue;
                        sInfo = sInfo.Substring(sInfo.IndexOf('/') + 1);     //将接收到的数据字符串再去掉 可用内存
                        sInfo = sInfo.Substring(sInfo.IndexOf('$') + 1);     //将接收到的数据字符串再去掉 内存总大小


                        while (!sInfo.StartsWith("$"))       //提取进程信息
                        {
                            String strPro;
                            strPro = sInfo.Substring(0, sInfo.IndexOf('&'));

                            int pos;
                            pos = strPro.IndexOf('#');   //进程名称#进程具体信息 查找该#
                            bool iter;
                            iter = serverInfo[i].mProcInfo.ContainsKey(strPro.Substring(0, pos));
                            if (iter)               //有该进程信息
                            {
                                if (strPro.Substring(pos + 1) == "NOT EXECUTED"&& serverInfo[i].mProcInfo[strPro.Substring(0, pos)] == true)                                   
                                {
                                    if (serverInfo[i].forbiddenPro.ContainsKey(strPro.Substring(0, pos)))
                                    {
                                        flag5 = true;
                                        logInfo.logcontent = "被禁用进程" + strPro.Substring(0, pos) + "停止执行";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    else
                                    {
                                        logInfo.logcontent = "进程" + strPro.Substring(0, pos) + "停止执行";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    serverInfo[i].mProcInfo[strPro.Substring(0, pos)] = false;  //设置该进程对应的状态为false
                                }
                                else if (strPro.Substring(pos + 1) != "NOT EXECUTED"&& serverInfo[i].mProcInfo[strPro.Substring(0, pos)] == false)                                   
                                {
                                    if (serverInfo[i].forbiddenPro.ContainsKey(strPro.Substring(0, pos)))
                                    {
                                        flag5 = false;
                                        logInfo.logcontent = "被禁用进程" + strPro.Substring(0, pos) + "开始启动";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Red);
                                    }
                                    else
                                    {
                                        logInfo.logcontent = "进程" + strPro.Substring(0, pos) + "开始执行";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    serverInfo[i].mProcInfo[strPro.Substring(0, pos)] = true; //设置该进程对应的状态为true        
                                }
                                else if (strPro.Substring(pos + 1) != "NOT EXECUTED" && serverInfo[i].mProcInfo[strPro.Substring(0, pos)] == true)
                                {
                                    if (serverInfo[i].forbiddenPro.ContainsKey(strPro.Substring(0, pos)))
                                    {
                                        flag5 = false;
                                    }
                                }
                            }
                            else             //没有该进程信息
                            {
                                if (strPro.Substring(pos + 1) == "NOT EXECUTED")
                                {
                                    serverInfo[i].mProcInfo.Add(strPro.Substring(0, pos), false);
                                    logInfo.logcontent = "进程" + strPro.Substring(0, pos) + "未执行";
                                    logInfo.maintype = "操作";
                                    logInfo.senctype = "设备";
                                    logInfo.WriteLog();
                                    string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                    AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Yellow);
                                }
                                else
                                {
                                    serverInfo[i].mProcInfo.Add(strPro.Substring(0, pos), true);
                                    if (serverInfo[i].forbiddenPro.ContainsKey(strPro.Substring(0, pos)))
                                    {
                                        flag5 = false;
                                        logInfo.logcontent = "被禁用进程" + strPro.Substring(0, pos) + "已启动";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Red);
                                    }
                                }
                            }
                            sInfo = sInfo.Substring(sInfo.IndexOf('&') + 1);
                        }
                        sInfo = sInfo.Substring(1);

                        while (!sInfo.StartsWith("$"))       //提取服务信息
                        {
                            String strSer;
                            strSer = sInfo.Substring(0, sInfo.IndexOf('&'));

                            int pos;
                            pos = strSer.IndexOf('#');   //服务名称#服务状态 查找该#
                            bool iter;
                            iter = serverInfo[i].mServInfo.ContainsKey(strSer.Substring(0, pos));
                            if (iter)               //有该服务信息
                            {
                                if (strSer.Substring(pos + 1) == "4"&& serverInfo[i].mServInfo[strSer.Substring(0, pos)] == false)                                   
                                {
                                    if (serverInfo[i].forbiddenPro.ContainsKey(strSer.Substring(0, pos)))
                                    {
                                        flag6 = false;
                                        logInfo.logcontent = "被禁用服务" + strSer.Substring(0, pos) + "已启动";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Red);
                                    }
                                    //设置该服务对应的状态为true
                                    else
                                    {
                                        logInfo.logcontent = "服务" + strSer.Substring(0, pos) + "开始运行";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    serverInfo[i].mServInfo[strSer.Substring(0, pos)] = true;
                                }
                                else if (strSer.Substring(pos + 1) == "1" && serverInfo[i].mServInfo[strSer.Substring(0, pos)] == true)                                  
                                {
                                    if (serverInfo[i].forbiddenPro.ContainsKey(strSer.Substring(0, pos)))
                                    {
                                        flag6 = true;
                                        logInfo.logcontent = "被禁用服务" + strSer.Substring(0, pos) + "停止";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Green);
                                    }
                                    else
                                    {
                                        logInfo.logcontent = "服务" + strSer.Substring(0, pos) + "停止";
                                        logInfo.maintype = "操作";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Red);
                                    }
                                    serverInfo[i].mServInfo[strSer.Substring(0, pos)] = false;//设置该服务对应的状态为false
                                }
                                else if (strSer.Substring(pos + 1) == "4" && serverInfo[i].mServInfo[strSer.Substring(0, pos)] == true)
                                {
                                    if (serverInfo[i].forbiddenPro.ContainsKey(strSer.Substring(0, pos)))
                                        flag6 = false;
                                }
                            }
                            else                   //没有该服务信息
                            {
                                if (strSer.Substring(pos + 1) == "1")
                                {
                                    serverInfo[i].mServInfo.Add(strSer.Substring(0, pos), false);
                                    logInfo.logcontent = "服务" + strSer.Substring(0, pos) + "未运行";
                                    logInfo.maintype = "操作";
                                    logInfo.senctype = "设备";
                                    logInfo.WriteLog();
                                    string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                    AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(), System.Drawing.Color.Yellow);
                                }
                                else if (strSer.Substring(pos + 1) == "4")
                                {
                                    serverInfo[i].mServInfo.Add(strSer.Substring(0, pos), true);
                                    if (serverInfo[i].forbiddenPro.ContainsKey(strSer.Substring(0, pos)))
                                    {
                                        flag6 = false;
                                        logInfo.logcontent = "被禁用服务" + strSer.Substring(0, pos) + "已启动";
                                        logInfo.maintype = "异常";
                                        logInfo.senctype = "设备";
                                        logInfo.WriteLog();
                                        string timett = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                                        AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timett, i.ToString(), System.Drawing.Color.Red);
                                    }
                                }
                            }
                            sInfo = sInfo.Substring(sInfo.IndexOf('&') + 1);
                        }

                        //提取流量信息      $接口#带宽#输入流量#输出流量#总流量#
                        String strTraffic;
                        strTraffic = recvdata;// allInfo[i].sInfo;
                        strTraffic = strTraffic.Substring(strTraffic.LastIndexOf('$') + 1);    //提取 接口#带宽#输入流量#输出流量#总流量#

                        serverInfo[i].netPortName = strTraffic.Substring(0, strTraffic.IndexOf('#'));    //提取 接口
                        strTraffic = strTraffic.Substring(strTraffic.IndexOf('#') + 1);                           //去掉 接口#

                        serverInfo[i].bandWidth = Convert.ToInt32(strTraffic.Substring(0, strTraffic.IndexOf('#')));    //提取 带宽
                        strTraffic = strTraffic.Substring(strTraffic.IndexOf('#') + 1);                                        //去掉 带宽#

                        serverInfo[i].inComingTraffic = Convert.ToDouble(strTraffic.Substring(0, strTraffic.IndexOf('#')));  //提取 输入流量
                        strTraffic = strTraffic.Substring(strTraffic.IndexOf('#') + 1);                                             //去掉 输入流量#

                        serverInfo[i].outGoingTraffic = Convert.ToDouble(strTraffic.Substring(0, strTraffic.IndexOf('#')));  //提取 输出流量
                        strTraffic = strTraffic.Substring(strTraffic.IndexOf('#') + 1);                                             //去掉 输出流量#

                        double tValue = Convert.ToDouble(strTraffic.Substring(0, strTraffic.IndexOf('#')));
                        //流量预警
                        if (tValue > serverInfo[i].trafficTH * 1024 && serverInfo[i].totalTraffic < serverInfo[i].trafficTH * 1024)
                        {
                            logInfo.logcontent = "流量超过门限值";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "主机";
                            logInfo.WriteLog();
                            flag4 = false;
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, (hostInfo.Count + i).ToString(), System.Drawing.Color.Yellow);

                        }
                        else if (tValue > serverInfo[i].trafficTH * 1024 && serverInfo[i].totalTraffic > serverInfo[i].trafficTH * 1024)
                        {
                            flag4 = false;
                        }
                        else if (tValue < serverInfo[i].trafficTH * 1024 && serverInfo[i].totalTraffic > serverInfo[i].trafficTH * 1024)
                        {
                            flag4 = true;
                            logInfo.logcontent = "流量恢复正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "主机";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, (hostInfo.Count + i).ToString(), System.Drawing.Color.Green);

                        }
                        else
                        {
                            flag4 = true;
                        }
                        serverInfo[i].totalTraffic = tValue;    //提取 总流量
                        if ((flag1 && flag2 && flag3 && flag4&&flag5&&flag6) == false)
                        {
                            serverInfo[i].bNormal = false;
                            if (IsVoiceOn)
                                Console.Beep();
                        }
                        else
                            serverInfo[i].bNormal = true;
                        break;
                    }
                }
                #endregion
                
            End: ;
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region 启动Ping机器的线程,每隔5秒Ping一次全部机器,添加主机后重新对网络进行拓扑,在活动设备列表中删除主机后将相应图标变灰
        private void PingHostRun()
        {
            try
            {
                ping_host = new Thread(new ThreadStart(this.PingHostsStart));
                ping_host.Start();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void PingHostsStart()
        {
            int nSec = 0;
            int mSec = 0;
            try
            {
                while (true)
                {
                    nSec++;
                    mSec++;
                    if (mSec >= 6)
                    {
                        UpdateSystemIfo();
                        mSec = 0;
                    }
                    Thread.Sleep(5000);
                    PingHosts();
                }
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void PingHosts()
        {
            try
            {
                for (int i = 0; i < hostInfo.Count; i++)
                {
                    //09-2-16加
                    LogInfo logInfo = new LogInfo();
                    logInfo.hostid = hostInfo[i].clientID;
                    logInfo.ipaddr = hostInfo[i].m_sIPInDatabase;
                    logInfo.hostname = hostInfo[i].hostName;

                    bool bPing = false;
                    if (hostInfo[i].bActive == false)
                    {
                        bPing = Ping(hostInfo[i].m_sIPInDatabase);
                        if (bPing)
                        {
                            //AddHost()
                            //09-2-16加
                            logInfo.logcontent = "线路恢复正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "网络";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, logInfo.senctype + hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, i.ToString(),System.Drawing.Color.Green);
                            m_topo.m_sNameOfDevices.Clear();
                            m_topo.m_sMacOfDevices.Clear();
                        }
                    }
                    else if (hostInfo[i].bActive == true)
                    {
                        bPing = Ping(hostInfo[i].m_sIPInDatabase);
                        if (!bPing)
                        {
                            //09-2-16加
                            logInfo.logcontent = "线路状态未知";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "网络";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, logInfo.senctype + hostInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr,i.ToString(), System.Drawing.Color.Yellow);
                            hostInfo[i].bActive = false;
                            String str = hostInfo[i].m_sIPInDatabase;
                        }
                    }
                }

                for (int i = 0; i < serverInfo.Count; i++)
                {
                    //09-2-16加
                    LogInfo logInfo = new LogInfo();
                    logInfo.hostid = serverInfo[i].clientID;
                    logInfo.ipaddr = serverInfo[i].m_sIPInDatabase;
                    logInfo.hostname = serverInfo[i].hostName;
                    int allid = i + hostInfo.Count;
                    bool bPing = false;
                    if (serverInfo[i].bActive == false)
                    {
                        bPing = Ping(serverInfo[i].m_sIPInDatabase);
                        if (bPing)
                        {
                            //AddHost()
                            //09-2-16加
                            logInfo.logcontent = "线路恢复正常";
                            logInfo.maintype = "操作";
                            logInfo.senctype = "网络";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, logInfo.senctype + serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(),System.Drawing.Color.Green);
                            m_topo.m_sNameOfDevices.Clear();
                            m_topo.m_sMacOfDevices.Clear();
                        }
                    }
                    else if (serverInfo[i].bActive == true)
                    {
                        bPing = Ping(serverInfo[i].m_sIPInDatabase);
                        if (!bPing)
                        {
                            //09-2-16加
                            logInfo.logcontent = "线路状态未知";
                            logInfo.maintype = "异常";
                            logInfo.senctype = "网络";
                            logInfo.WriteLog();
                            string timeStr = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "/" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
                            AddListViewItem(this.listViewAlarmInfo, logInfo.senctype + serverInfo[i].m_sIPInDatabase + logInfo.logcontent, timeStr, allid.ToString(), System.Drawing.Color.Yellow);
                            serverInfo[i].bActive = false;
                            String str = serverInfo[i].m_sIPInDatabase;
                        }
                    }
                }
                //状态统计日志，每隔1小时记录一次状态日志
                if(TimeStamp ==0 || TimeStamp % (statInterval*12) ==0)
                    writeStaToLog();
                TimeStamp++;
                this.panelEx1.Refresh();

            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region 1秒计时器,判断主机未接收到数据的次数,超过5次进行更新
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                lock (this)
                {
                    for (int i = 0; i < hostInfo.Count; i++)
                    {
                        if (hostInfo[i].bActive == true &&
                            hostInfo[i].lCurRecvLen - hostInfo[i].lPreRecvLen == 0)
                        {
                            if (hostInfo[i].lCurRecvLen != 0)
                            {
                                hostInfo[i].nCount++;
                            }
                        }
                        else if (hostInfo[i].bActive == true &&
                            hostInfo[i].lCurRecvLen - hostInfo[i].lPreRecvLen != 0)
                        {
                            if (hostInfo[i].lPreRecvLen == 0)
                            {
                                //重绘拓扑?
                            }
                            hostInfo[i].nCount = 0;
                            hostInfo[i].lPreRecvLen = hostInfo[i].lCurRecvLen;
                        }
                        if (hostInfo[i].nCount == 5)
                        {
                            hostInfo[i].lCurRecvLen = 0;
                            hostInfo[i].lPreRecvLen = 0;
                            hostInfo[i].nCount = 0;
                            hostInfo[i].sInfo = "";
                            hostInfo[i].cpuValue = 0;
                            hostInfo[i].diskValue = 50000;
                            hostInfo[i].memValue = 5000;
                        }
                    }

                    for (int i = 0; i < serverInfo.Count; i++)
                    {
                        if (serverInfo[i].bActive == true &&
                            serverInfo[i].lCurRecvLen - serverInfo[i].lPreRecvLen == 0)
                        {
                            if (serverInfo[i].lCurRecvLen != 0)
                            {
                                serverInfo[i].nCount++;
                            }
                        }
                        else if (serverInfo[i].bActive == true &&
                            serverInfo[i].lCurRecvLen - serverInfo[i].lPreRecvLen != 0)
                        {
                            if (serverInfo[i].lPreRecvLen == 0)
                            {
                                //重绘拓扑?
                            }
                            serverInfo[i].nCount = 0;
                            serverInfo[i].lPreRecvLen = serverInfo[i].lCurRecvLen;
                        }
                        if (serverInfo[i].nCount == 5)
                        {
                            serverInfo[i].lCurRecvLen = 0;
                            serverInfo[i].lPreRecvLen = 0;
                            serverInfo[i].nCount = 0;
                            serverInfo[i].sInfo = "";
                            serverInfo[i].cpuValue = 0;
                            serverInfo[i].diskValue = 50000;
                            serverInfo[i].memValue = 5000;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
                MessageBoxEx.Show(str);
            }
        }
        #endregion

        #region 启动程序时 根据配置文件判断 删除超出保存期限的日志
        private void DeleteRedundantLog()
        {
            try
            {
                year = DateTime.Now.Year;
                month = DateTime.Now.Month;
                day = DateTime.Now.Day;

                if (m_savetime == "Y")
                {
                    //删除日志(删除year-1之前的所有日志,也包括year-1这一年的)
                    if (Directory.Exists(MainForm.m_LogPath ))
                    {
                        yearfoldEntries = Directory.GetDirectories(MainForm.m_LogPath );
                        foreach (String yearfold in yearfoldEntries)                  //获取每个年文件夹
                        {
                            yearstr = yearfold.Substring(yearfold.LastIndexOf('\\') + 1);
                            foldyear = Convert.ToInt32(yearstr.Substring(0, yearstr.IndexOf('年')));

                            if (foldyear < year)
                            {
                                monthfoldEntries = Directory.GetDirectories(yearfold);
                                foreach (String monthfold in monthfoldEntries)             //获取每个月文件夹
                                {
                                    fileEntries = Directory.GetFiles(monthfold);           //获取月文件夹中的每一个日志文件
                                    foreach (String fileName in fileEntries)
                                    {
                                        File.Delete(fileName);
                                    }
                                    Directory.Delete(monthfold);
                                }
                                Directory.Delete(yearfold);
                            }
                        }
                    }
                }//if (m_savetime == "Y")结束
                else if (m_savetime == "M")
                {
                    //删除日志
                    if (month == 1)         //删除上一年12月之前的,即删除上一年之前所有的
                    {
                        if (Directory.Exists(MainForm.m_LogPath ))
                        {
                            yearfoldEntries = Directory.GetDirectories(MainForm.m_LogPath);
                            foreach (String yearfold in yearfoldEntries)                  //获取每个年文件夹
                            {
                                yearstr = yearfold.Substring(yearfold.LastIndexOf('\\') + 1);
                                foldyear = Convert.ToInt32(yearstr.Substring(0, yearstr.IndexOf('年')));

                                if (foldyear < year)
                                {
                                    monthfoldEntries = Directory.GetDirectories(yearfold);
                                    foreach (String monthfold in monthfoldEntries)             //获取每个月文件夹
                                    {
                                        fileEntries = Directory.GetFiles(monthfold);           //获取月文件夹中的每一个日志文件
                                        foreach (String fileName in fileEntries)
                                        {
                                            File.Delete(fileName);
                                        }
                                        Directory.Delete(monthfold);
                                    }
                                    Directory.Delete(yearfold);
                                }
                            }
                        }
                    }
                    else          //删除本年内前一个月之前的
                    {
                        if (Directory.Exists(MainForm.m_LogPath ))
                        {
                            yearfoldEntries = Directory.GetDirectories(MainForm.m_LogPath );
                            foreach (String yearfold in yearfoldEntries)                  //获取每个年文件夹
                            {
                                yearstr = yearfold.Substring(yearfold.LastIndexOf('\\') + 1);
                                foldyear = Convert.ToInt32(yearstr.Substring(0, yearstr.IndexOf('年')));

                                if (foldyear < year)
                                {
                                    monthfoldEntries = Directory.GetDirectories(yearfold);
                                    foreach (String monthfold in monthfoldEntries)             //获取每个月文件夹
                                    {
                                        fileEntries = Directory.GetFiles(monthfold);           //获取月文件夹中的每一个日志文件
                                        foreach (String fileName in fileEntries)
                                        {
                                            File.Delete(fileName);
                                        }
                                        Directory.Delete(monthfold);
                                    }
                                    Directory.Delete(yearfold);
                                    continue;
                                }

                                if (foldyear == year)
                                {
                                    monthfoldEntries = Directory.GetDirectories(yearfold);
                                    foreach (String monthfold in monthfoldEntries)             //获取每个月文件夹
                                    {
                                        monthstr = monthfold.Substring(monthfold.LastIndexOf('\\') + 1);
                                        foldmonth = Convert.ToInt32(monthstr.Substring(0, monthstr.IndexOf('月')));

                                        if (foldmonth < month) //20100601 Ding Yiming 去掉"-1"
                                        {
                                            fileEntries = Directory.GetFiles(monthfold);           //获取月文件夹中的每一个日志文件
                                            foreach (String fileName in fileEntries)
                                            {
                                                File.Delete(fileName);
                                            }
                                            Directory.Delete(monthfold);
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                }//if (m_savetime == "M") 结束
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region  接收网络时间并且分发到局域网


        /// <summary>
        /// 读取配置文件指定字段信息值
        /// </summary>
        /// <param name="filename">配置文件名</param>
        /// <param name="strname">所要读取的配置文件字段</param>
        /// <returns>字段值结果字符串</returns>
        public static string ReadConFile(string filename, string strname)
        {
            MessageBoxEx.UseSystemLocalizedString = true;
            string nowPath = Directory.GetCurrentDirectory();
            string conf_path = nowPath + "\\" + filename;
            string outstring = "";
            bool flag = false;
            try
            {
                using (StreamReader sr = File.OpenText(conf_path))
                {
                    while (sr.Peek() >= 0)
                    {
                        outstring = sr.ReadLine();
                        if (outstring.Trim().StartsWith(strname.ToString()))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag)
                        outstring = outstring.Substring(outstring.IndexOf('=') + 1);
                    else
                        outstring = "";

                    return outstring;
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show("指定的ini配置文件不存在或出现错误，请检查！" + Environment.NewLine +
                    "异常:" + ex.Message);
                return "";
            }
        }
        /// <summary>
        /// 接收网络时间信息
        /// </summary>
        public void RecvFormTimeServer()
        {
            /* 用于调时候的稳定版本
            string ipAddr = ReadConFile("CONFIG\\TimeConfig.ini", "Multicasrip");
            int port = Convert.ToUInt16(ReadConFile("CONFIG\\TimeConfig.ini", "Multicastport"));
            */
            m_timesock = new MulticastSocket("234.5.6.7", 9999);  //调试用IP和Port
            Thread t_timerecv = new Thread(new ThreadStart(this.RecvDataRun));
            t_timerecv.Start();
        }

        public void RecvDataRun()
        {
            MessageBoxEx.UseSystemLocalizedString = true;
            string recvdata = Encoding.Default.GetString(new byte[2 * 1024]);
            string timestr = "";
            long recvlen = 0;
            try
            {
                while (true)
                {
                    recvlen = m_timesock.ReceiveData(ref recvdata);
                    if (recvlen == 0) continue;
                    AnalyTimeInfo(recvdata, ref timestr); //解析时间数据
                    bdsock.Boradcast_Send(timestr);  //对时间信息进行局域网广播
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(ex.ToString());
            }
        }

        public void AnalyTimeInfo(string orgStr, ref string timestr)
        {
            //解析时间格式字符串 "yyyy-mm-dd-hh-mm-ss-ms"
            timestr = orgStr;

        }
        #endregion

        #region 在关闭窗体前显示确认对话框,若确认关闭则同时关闭相关的进程
        //在关闭窗体前显示确认对话框
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            MessageBoxEx.UseSystemLocalizedString = true;
            try
            {
                if (doesNotExist)//20100601 Ding Yiming+ "开启一个的问题"
                {
                    if (MessageBoxEx.Show("将要退出，是否继续？", "确认退出", MessageBoxButtons.OKCancel,MessageBoxIcon.Information) == DialogResult.OK)
                    {
                        //注意关闭的顺序不能颠倒
                        #region 关闭视频源显示的进程
                        //如果视频源显示的进程仍在运行,则关闭
                        System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcesses();
                        foreach (System.Diagnostics.Process myProcessigle in myProcesses)
                        {
                            if ("TSView" == myProcessigle.ProcessName)          //注:进程名为VideoPlay,不应写为VideoPlay.exe;
                            {
                                myProcessigle.Kill();
                            }
                        }
                        #endregion

                        #region 关闭进程
                        System.Diagnostics.Process[] myPros = System.Diagnostics.Process.GetProcesses();
                        foreach (System.Diagnostics.Process myProcessigle in myPros)
                        {
                            if ("MonitorAndSet" == myProcessigle.ProcessName)          //关闭进程MonitorAndSet.exe
                            {
                                myProcessigle.Kill();
                            }
                            if ("系统监控" == myProcessigle.ProcessName)          //关闭进程MonitorAndSet.exe
                            {
                                myProcessigle.Kill();
                            }
                        }
                        #endregion
                        e.Cancel = false;
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion

        #region 内部设备各种图标双击响应函数
        //拓扑图中图标双击响应
        private void ShowDataSourceForm()
        {
            try
            {
                if (DataShowForm == null || DataShowForm.IsDisposed)
                {
                    DataShowForm = new DataShow();
                    DataShowForm.Owner = this;
                }
                DataShowForm.Show();
                DataShowForm.WindowState = FormWindowState.Normal;
                DataShowForm.Activate();
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }
        #endregion
        
        #region 实时监听数据源是否有数据
        //每隔5秒从测量设备的IP地址和端口号接收数据、判断是否有数据
        public void GetMsDevState()
        {
            try
            {
                receive_data = new Thread(new ThreadStart(this.ReceiveDataStart));
                receive_data.Start();
                threadstate = true;
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void ReceiveDataStart()
        {
            try
            {
                while (true)
                {
                    string recvdata = "";
                    m_socket.Clear();
                    Thread.Sleep(2000);  //隔2秒刷新一次
                    for (int i = 0; i < m_msdevice.Count; i++)
                    {
                        if (m_msdevice[i].devIP != "" && m_msdevice[i].devPort > 0 && m_msdevice[i].devPort < 65536)  //20090608gai
                        {
                            int mudr = Convert.ToInt32(m_msdevice[i].devIP.Substring(0, m_msdevice[i].devIP.IndexOf('.')));
                            if (mudr >= 224 && mudr <= 239)
                            {
                                m_socket.Add(new MulticastSocket(m_msdevice[i].devIP, m_msdevice[i].devPort));
                            }
                            else
                            {
                                m_socket.Add(new MulticastSocket());
                            }
                        }
                        else
                        {
                            m_socket.Add(new MulticastSocket());
                        }
                    }
                    for (int i = 0; i < m_socket.Count; i++)
                    {
                        m_socket[i].lPreRecvLen = m_socket[i].lCurRecvLen;
                        m_socket[i].lCurRecvLen += m_socket[i].ReceiveData(ref recvdata);//
                        recvdata = "";
                    }
                    AnalyzeState();
                }
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        private void AnalyzeState()
        {
            try
            {
                ///获取所有设备状态和流量
                for (int i = 0; i < m_socket.Count; i++)
                {
                    if (m_socket[i].lCurRecvLen > m_socket[i].lPreRecvLen && m_msdevice[i].state == false) //从"无数据"到"有数据"时的流量变化
                    {
                        m_msdevice[i].state = true;
                        m_msdevice[i].Traffic = (double)((m_socket[i].lCurRecvLen - m_socket[i].lPreRecvLen) / (1024.0));
                    }
                    else if (m_socket[i].lCurRecvLen > m_socket[i].lPreRecvLen && m_msdevice[i].state == true)   //一直有数据时的流量变化
                    {
                        m_msdevice[i].Traffic = (double)((m_socket[i].lCurRecvLen - m_socket[i].lPreRecvLen) / (1024.0));
                    }
                    else if (m_socket[i].lCurRecvLen == m_socket[i].lPreRecvLen && m_msdevice[i].state == true)
                    {
                        m_msdevice[i].state = false;
                        m_msdevice[i].Traffic = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                String str = ex.ToString();
            }
        }

        #endregion

        #region 判断设备名称所占像素数
        public int DeviceCodeLength(string name)
        {
            int length = 0;
            for (int i = 0; i < name.Length;i++ )
            {
                if (name.Substring(i, 1) == "1" || name.Substring(i, 1) == "i")
                {
                    length = length + 6;
                }
                else
                {
                    length = length + 12;
                }
            }
            return length;
        }
        #endregion

        #region 内部设备绘图
        private void Inner_Paint(object sender, PaintEventArgs e)
        {
            try 
            {
                Graphics displayGraphics = e.Graphics;
                Image i = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
                Graphics g = Graphics.FromImage(i);

                MyPen.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;  //点划线
                MyQTPen.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;  //点划线

                //指挥控制大厅
                ZHKZDT_LeftUp = new Point(leftupx + 1, leftupy + 5);        //指挥控制大厅显示区域的左上角点
                ZHKZDT_RightUp = new Point(leftupx + rewidth - 5, leftupy + 5);      //指挥控制大厅显示区域的右上角点
                ZHKZDT_RightDown = new Point(leftupx + rewidth - 5, leftupy + reheight - 1);    //指挥控制大厅显示区域的右下角点
                ZHKZDT_LeftDown = new Point(leftupx + 1, leftupy + reheight - 1);      //指挥控制大厅显示区域的左下角点
                Point TextPoint1 = new Point(ZHKZDT_LeftUp.X + 1, ZHKZDT_LeftUp.Y - 16); //"指挥控制大厅"的输出点
                Point TextPoint11 = new Point(ZHKZDT_LeftUp.X + 1, ZHKZDT_LeftUp.Y + 3); //"席位机"的输出点
                //指挥控制大厅显示区域
                g.DrawLine(MyPen, ZHKZDT_LeftUp, ZHKZDT_RightUp);
                g.DrawLine(MyPen, ZHKZDT_RightUp, ZHKZDT_RightDown);
                g.DrawLine(MyPen, ZHKZDT_RightDown, ZHKZDT_LeftDown);
                g.DrawLine(MyPen, ZHKZDT_LeftDown, ZHKZDT_LeftUp);
                //g.DrawString("指挥控制大厅", MyBigFont, Brushes.Blue, TextPoint1);
                g.DrawString("席\r\n位\r\n机\r\n区", MyFont, Brushes.Blue, TextPoint11);

                
                //席位机
                //SeatHeight = (ZHKZDT_LeftDown.Y - ZHKZDT_LeftUp.Y) / 5 * 3;
                Point SEAT_LeftUp = ZHKZDT_LeftUp;
                Point SEAT_RightUp = ZHKZDT_RightUp;
                Point SEAT_RightDown = new Point(leftupx + rewidth - 5, leftupy + 5 + SeatHeight);
                Point SEAT_LeftDown = new Point(leftupx + 1, leftupy + 5 + SeatHeight);
                g.DrawLine(MyPen, SEAT_LeftDown, SEAT_RightDown);

                //交换机
                QT_LeftUp = SEAT_LeftDown;        //主机房显示区域的左上角点
                QT_RightUp = SEAT_RightDown;      //主机房显示区域的右上角点
                QT_RightDown = new Point(SEAT_RightDown.X, SEAT_RightDown.Y + QtHeight);    //主机房显示区域的右下角点
                QT_LeftDown = new Point(SEAT_LeftDown.X, SEAT_LeftDown.Y + QtHeight);      //主机房显示区域的左下角点
                Point TextPoint2 = new Point(QT_LeftUp.X, QT_LeftUp.Y + 3); //"主机房"的输出点


                //交换机显示区域
                g.DrawLine(MyQTPen, QT_RightDown, QT_LeftDown);
                g.DrawString("交\r\n换\r\n机\r\n区", MyFont, Brushes.Blue, TextPoint2);

                //集群服务器区域
                SAN_LeftUp = QT_LeftDown;
                SAN_RightUp = QT_RightDown;
                SAN_LeftDown = ZHKZDT_LeftDown;
                SAN_RightDown = ZHKZDT_RightDown;

                Point TextPoint3 = new Point(SAN_LeftUp.X, SAN_LeftUp.Y + 3); //"集群服务器"的输出点
                g.DrawString("服\r\n务\r\n器\r\n区", MyFont, Brushes.Blue, TextPoint3);
                DrawSwitch(g);
                DrawSeats(g);
                DrawServer(g);
                DrawLines(g);
                //DrawSAN(g);
                ///绘制席位机连线
                //DrawLines(g);
                DrawServerName(g);
                displayGraphics.DrawImage(i, ClientRectangle);
                i.Dispose();
            }
            catch(Exception ex)
            {
                MessageBoxEx.Show(ex.ToString());
            }
            
        }
        #endregion

        #region 画交换机、席位、服务器

        private void DrawSwitch(Graphics g)
        {
            if (switchInfo.Count > 0)
            {
                for (int k = 0; k < switchInfo.Count; k++)
                {
                    if (switchInfo[k].bActive == true && switchInfo[k].lCurRecvLen == 0)
                    {
                        g.DrawImage(Topology.image_switchYellow, switchInfo[k].XLocation, switchInfo[k].YLocation, width_switch, height_switch);
                    }
                    else if (switchInfo[k].bActive == true && switchInfo[k].lCurRecvLen != 0)
                    {
                        if (switchInfo[k].bNormal == true)
                        {
                            g.DrawImage(Topology.image_switchBlue, switchInfo[k].XLocation, switchInfo[k].YLocation, width_switch, height_switch);
                        }
                        else if (switchInfo[k].bNormal == false)
                        {
                            g.DrawImage(Topology.image_switchRed, switchInfo[k].XLocation, switchInfo[k].YLocation, width_switch, height_switch);
                        }
                    }
                    else if (switchInfo[k].bActive == false)
                    {
                        g.DrawImage(Topology.image_switchGray, switchInfo[k].XLocation, switchInfo[k].YLocation, width_switch, height_switch);
                    }
                }
            }
        }
        /// <summary>
        /// 画席位
        /// </summary>
        /// <param name="hostInfo"></param>
        /// <param name="g"></param>
        private void DrawSeats(Graphics g)
        {
            if (hostInfo.Count > 0)
            {
                for (int k = 0; k < hostInfo.Count; k++)
                {
                    if (hostInfo[k].bActive == true && hostInfo[k].lCurRecvLen == 0)
                    {
                        g.DrawImage(Topology.image_HostYellow, hostInfo[k].XLocation, hostInfo[k].YLocation, width_seat, height_seat);
                    }
                    else if (hostInfo[k].bActive == true && hostInfo[k].lCurRecvLen != 0)
                    {
                        if (hostInfo[k].bNormal == true)
                        {
                            g.DrawImage(Topology.image_HostBlue, hostInfo[k].XLocation, hostInfo[k].YLocation, width_seat, height_seat);
                        }
                        else if (hostInfo[k].bNormal == false)
                        {
                            g.DrawImage(Topology.image_HostRed, hostInfo[k].XLocation, hostInfo[k].YLocation, width_seat, height_seat);
                        }
                    }
                    else if (hostInfo[k].bActive == false)
                    {
                        g.DrawImage(Topology.image_HostGray, hostInfo[k].XLocation, hostInfo[k].YLocation, width_seat, height_seat);
                    }
                }
            }
        }
        /// <summary>
        /// 画数据库服务器
        /// </summary>
        /// <param name="hostInfo"></param>
        /// <param name="g"></param>
        private void DrawServer(Graphics g) 
        {
            if (serverInfo.Count > 0)
            {
                for (int k = 0; k < serverInfo.Count; k++)
                {
                    if (serverInfo[k].bActive == true && serverInfo[k].lCurRecvLen == 0)
                    {
                        g.DrawImage(Topology.image_switchYellow, serverInfo[k].XLocation, serverInfo[k].YLocation, width_server, height_server);
                    }
                    else if (serverInfo[k].bActive == true && serverInfo[k].lCurRecvLen != 0)
                    {
                        if (serverInfo[k].bNormal == true)
                        {
                            g.DrawImage(Topology.image_serverBlue, serverInfo[k].XLocation, serverInfo[k].YLocation, width_server, height_server);
                        }
                        else if (serverInfo[k].bNormal == false)
                        {
                            g.DrawImage(Topology.image_serverRed, serverInfo[k].XLocation, serverInfo[k].YLocation, width_server, height_server);
                        }
                    }
                    else if (serverInfo[k].bActive == false)
                    {
                        g.DrawImage(Topology.image_serverGray, serverInfo[k].XLocation, serverInfo[k].YLocation, width_server, height_server);
                    }
                }
            }
        }
        
        #endregion

        #region  设置交换机、席位机、服务器位置 
      
        /*根据交换机位置设置席位机、服务器位置
         **/
        public void SetLocation()
        {
            try
            {
                setSwitchLocation();
                setHostAndServerLocation();
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(ex.ToString());
            }
        }
        
        /**获取交换机之间的空隙宽度
         * 如果宽度小于交换机图片的宽度就将图片大小缩小为原来的3/4
         * 同时修改席位机、服务器图片的大小
         ****/
        public  int getSwitchSpace(int width, int count_switch)
        {

            int space_switch = (width - width_switch * count_switch) / (count_switch + 1);
            if (space_switch < width_switch)
            {
                width_switch = width_switch * 3 / 4;
                height_switch = height_switch * 3 / 4;
                width_seat = width_seat * 3 / 4;
                height_seat = height_seat * 3 / 4;
                width_server = width_server * 3 / 4;
                height_server = height_server * 3 / 4;
                space_switch = getSwitchSpace(width, count_switch);
            }
            return space_switch;
        }
       
        /*设置交换机位置
         */ 
        public  void setSwitchLocation()
        {
            try
            {
                getSwitchLevel();
                int count_switch = MainForm.switchInfo.Count;//交换机数目
                if (count_switch == 1)//只有1个交换机,整个界面划分2：1：2
                {
                    SeatHeight = reheight /5 * 2-25;
                    QtHeight = reheight / 5 ;
                    switchInfo[0].XLocation = rewidth / 2 - width_switch/2;
                    switchInfo[0].YLocation = reheight / 2 + height_switch/2;
                    switchlist.Add(switchInfo[0]);
                    switchChildCount.Add(1);
                }
                else if (count_switch > 1)//整个界面划分5：2：3
                {
                    SeatHeight = reheight / 2-25;
                    QtHeight = reheight / 5;
                    int space_switch = (rewidth-width_switch*count_switch) / (count_switch + 1);//getSwitchSpace(rewidth, count_switch);
                    int space_height = (reheight / 5 - height_switch * switch_level) / (switch_level + 1);
                    int id = MainForm.switchInfo[0].clientID;
                    MainForm.switchInfo[0].XLocation = space_switch;
                    MainForm.switchInfo[0].YLocation = reheight / 10 * 5 + space_height;
                    switchlist.Add(switchInfo[0]);
                   
                    GetSwitchLocation(id, count_switch, space_switch, space_height);
                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        /*获取交换机层数
         * 
         */
        public void getSwitchLevel() 
        {
            try 
            {
                string sql = "select sum(count1-2) from (select router_serverid, count(router_serverid) count1 "
                    +"from t_router t where router_connport = 0 group by router_serverid) a where a.count1>2";
                Database db = new Database();
                DataSet ds = db.GetDataSet(sql);
                if (ds != null && ds.Tables.Count > 0)
                {
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows[0][0].ToString() == "")
                    {
                        switch_level = 1;
                    }
                    else
                        switch_level = int.Parse(dt.Rows[0][0].ToString())+1;
                }
            }
            catch(Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        /*计算交换机位置//循环迭代
         */ 
        private void GetSwitchLocation(int id, int count_switch, int space_switch, int space_height) 
        {
            try 
            {
                string sql = "SELECT * FROM T_ROUTER WHERE ROUTER_CONNPORT = 0 and  ROUTER_SERVERID =  " + id;
                Database db = new Database();
                DataSet ds = db.GetDataSet(sql);
                if(ds!=null && ds.Tables.Count>0)
                {
                    DataTable dt = ds.Tables[0];
                   
                    List<DataRow> list = new List<DataRow>();
                    int n = dt.Rows.Count;//n一定大于0
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < count_switch; j++)
                        {
                            if (MainForm.switchInfo[j].clientID == int.Parse(dt.Rows[i]["ROUTER_CONNID"].ToString()) )
                            {
                                int count_switch_now = switchlist.Count;
                                bool exit = false;
                                for (int k = 0; k < count_switch_now;k++ )
                                {
                                    if (switchlist[k].clientID == MainForm.switchInfo[j].clientID)
                                    {
                                        exit = true;
                                        break;
                                    }
                                }
                                if(!exit)
                                {
                                    list.Add(dt.Rows[i]);
                                }
                            }
                        }
                    }
                    int m =list.Count;
                    switchChildCount.Add(m);
                    for (int i = 0; i < m; i++)
                    {
                        for (int j = 0; j < count_switch; j++)
                        {
                            if (MainForm.switchInfo[j].clientID == int.Parse(list[i]["ROUTER_CONNID"].ToString()))
                            {
                                if (i == 0)//交换机第一个子节点，行不变，列加1
                                {
                                    MainForm.switchInfo[j].XLocation = switch_collunm
                                    * (width_switch + space_switch) + space_switch;
                                    switch_collunm++;
                                    MainForm.switchInfo[j].YLocation = reheight / 2 + space_height
                                        + (switch_row-1) * (height_switch + space_height);
                                    switchlist.Add(switchInfo[j]);
                                    GetSwitchLocation(MainForm.switchInfo[j].clientID,count_switch,space_switch,space_height);
                                }
                                else//交换机子节点（除第一个），行加1，列加1
                                {
                                    MainForm.switchInfo[j].XLocation = switch_collunm
                                    * (width_switch + space_switch) + space_switch;
                                    switch_collunm++;
                                    switch_row++;
                                    MainForm.switchInfo[j].YLocation = reheight / 2 + space_height
                                         + (switch_row - 1) * (height_switch + space_height);
                                    switchlist.Add(switchInfo[j]);
                                    GetSwitchLocation(MainForm.switchInfo[j].clientID, count_switch, space_switch,space_height);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }
      
        /** 设置席位机和服务器位置
        */
        private void setHostAndServerLocation() 
        {
            try
            {
                int switch_count = MainForm.switchInfo.Count;//交换机数目
                if (switch_count == 1)
                {
                    SetHostLocation_1();
                    SetServerLocation_1();
                }
                else
                {
                    for (int i = 0; i < switch_count; i++)
                    {
                        int id = switchInfo[i].clientID;
                        int x = switchInfo[i].XLocation + width_switch / 2;

                        SetHostLocation_2(id, x);
                        SetServerLocation_2(id, x);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }
        
        /**设置席位机位置 同时获取席位机之间连线（只有一台交换机） 
        */
        private void SetHostLocation_1() 
        {
            try
            {
                int host_count = MainForm.hostInfo.Count;
                if (host_count > 0 && host_count <= 10)
                {
                    int space_seat = (rewidth - width_seat * host_count) / (host_count + 1);
                    for (int i = 0; i < host_count; i++)
                    {
                        hostInfo[i].XLocation = space_seat + i * (width_seat + space_seat);
                        hostInfo[i].YLocation = reheight / 5 - height_seat;

                        List<Point> list = new List<Point>();
                        Point p1 = new Point();
                        p1.X = hostInfo[i].XLocation + width_seat/2;
                        p1.Y = hostInfo[i].YLocation + height_seat;
                        list.Add(p1);
                        Point p2 = new Point();
                        p2.X = p1.X;
                        p2.Y = p1.Y + height_seat;
                        list.Add(p2);
                        list_lines_seats1.Add(list);
                    }

                    List<Point> list1 = new List<Point>();
                    Point p11 = new Point();
                    p11.X = hostInfo[0].XLocation + width_seat / 2;
                    p11.Y = hostInfo[0].YLocation + height_seat * 2;
                    list1.Add(p11);
                    Point p22 = new Point();
                    p22.X = hostInfo[host_count - 1].XLocation + width_seat / 2;
                    p22.Y = p11.Y;
                    list1.Add(p22);
                    list_lines_seats1.Add(list1);

                }
                else if (host_count > 10)
                {
                    if (host_count % 2 == 1)
                    {
                        int count_seat_top = 0;
                        if (((host_count - 1) / 2) % 2 == 1)
                        {
                            count_seat_top = (host_count - 1) / 2;
                        }
                        else 
                        {
                            count_seat_top = (host_count + 1) / 2;
                        }
                        
                        int space_seat_top = (rewidth - width_seat * count_seat_top) / (count_seat_top + 1);
                        for (int i = 0; i < count_seat_top; i++)
                        {
                            hostInfo[i].XLocation = space_seat_top + i * (width_seat + space_seat_top);
                            hostInfo[i].YLocation = reheight  / 5 - height_seat * 2;

                            List<Point> list = new List<Point>();
                            Point p1 = new Point();
                            p1.X = hostInfo[i].XLocation + width_seat / 2;
                            p1.Y = hostInfo[i].YLocation + height_seat;
                            list.Add(p1);
                            Point p2 = new Point();
                            p2.X = p1.X;
                            p2.Y = p1.Y + height_seat;
                            list.Add(p2);
                            list_lines_seats1.Add(list);
                        }
                        int count_seat_bottom = host_count - count_seat_top;
                        int space_seat_bottom = (rewidth - width_seat * count_seat_bottom) / (count_seat_bottom + 1);
                        for (int j = 0; j < count_seat_bottom; j++)
                        {
                            hostInfo[j + count_seat_top].XLocation = space_seat_bottom + j * (width_seat + space_seat_bottom);
                            hostInfo[j + count_seat_top].YLocation = reheight / 5 + height_seat;

                            List<Point> list = new List<Point>();
                            Point p1 = new Point();
                            p1.X = hostInfo[j + count_seat_top].XLocation + width_seat / 2;
                            p1.Y = hostInfo[j + count_seat_top].YLocation;
                           
                            Point p2 = new Point();
                            p2.X = p1.X;
                            p2.Y = p1.Y - height_seat;
                            list.Add(p2); 
                            list.Add(p1);
                            list_lines_seats1.Add(list);
                        }

                        if (count_seat_top > count_seat_bottom)
                        {
                            List<Point> list1 = new List<Point>();
                            Point p11 = new Point();
                            p11.X = hostInfo[0].XLocation + width_seat / 2;
                            p11.Y = hostInfo[0].YLocation + height_seat * 2;
                            list1.Add(p11);
                            Point p22 = new Point();
                            //p22.X = hostInfo[host_count - 1].XLocation + width_seat / 2;
                            p22.X = hostInfo[count_seat_top - 1].XLocation + width_seat / 2;
                            p22.Y = p11.Y;
                            list1.Add(p22);
                            list_lines_seats1.Add(list1);
                        }
                        else 
                        {
                            List<Point> list1 = new List<Point>();
                            Point p11 = new Point();
                            p11.X = hostInfo[count_seat_top].XLocation + width_seat / 2;
                            p11.Y = hostInfo[count_seat_top].YLocation - height_seat;
                            list1.Add(p11);
                            Point p22 = new Point();
                            p22.X = hostInfo[host_count - 1].XLocation + width_seat / 2;
                            p22.Y = p11.Y;
                            list1.Add(p22);
                            list_lines_seats1.Add(list1);
                        }
                    }
                    else
                    {
                        if ((host_count / 2) % 2 == 1)
                        {
                            int count_seat_top = host_count / 2 + 1;
                            int space_seat_top = (rewidth - width_seat * count_seat_top) / (count_seat_top + 1);
                            for (int i = 0; i < count_seat_top; i++)
                            {
                                hostInfo[i].XLocation = space_seat_top + i * (width_seat + space_seat_top);
                                hostInfo[i].YLocation = reheight / 5 - height_seat * 2;
                                
                                List<Point> list = new List<Point>();
                                Point p1 = new Point();
                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                p1.Y = hostInfo[i].YLocation + height_seat;
                                list.Add(p1);
                                Point p2 = new Point();
                                p2.X = p1.X;
                                p2.Y = p1.Y + height_seat;
                                list.Add(p2);
                                list_lines_seats1.Add(list);
                            }
                            int count_seat_bottom = host_count / 2 - 1;
                            int space_seat_bottom = (rewidth - width_seat * count_seat_bottom) / (count_seat_bottom + 1);
                            for (int j = 0; j < count_seat_bottom; j++)
                            {
                                hostInfo[j + count_seat_top].XLocation = space_seat_bottom + j * (width_seat + space_seat_bottom);
                                hostInfo[j + count_seat_top].YLocation = reheight  / 5 + height_seat;


                                List<Point> list = new List<Point>();
                                Point p1 = new Point();
                                p1.X = hostInfo[j + count_seat_top].XLocation + width_seat / 2;
                                p1.Y = hostInfo[j + count_seat_top].YLocation;
                                
                                Point p2 = new Point();
                                p2.X = p1.X;
                                p2.Y = p1.Y - height_seat;
                                list.Add(p2);
                                list.Add(p1);
                                list_lines_seats1.Add(list);
                            }
                            List<Point> list1 = new List<Point>();
                            Point p11 = new Point();
                            p11.X = hostInfo[0].XLocation + width_seat / 2;
                            p11.Y = hostInfo[0].YLocation + height_seat * 2;
                            list1.Add(p11);
                            Point p22 = new Point();
                            p22.X = hostInfo[count_seat_top - 1].XLocation + width_seat / 2;
                            p22.Y = p11.Y;
                            list1.Add(p22);
                            list_lines_seats1.Add(list1);
                        }
                        else
                        {
                            int count_seat_top = host_count / 2;
                            int space_seat_top = (rewidth - width_seat * count_seat_top) / (count_seat_top + 1);
                            for (int i = 0; i < count_seat_top; i++)
                            {
                                hostInfo[i].XLocation = space_seat_top + i * (width_seat + space_seat_top);
                                hostInfo[i].YLocation = reheight / 5 - height_seat * 2;

                                List<Point> list = new List<Point>();
                                Point p1 = new Point();
                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                p1.Y = hostInfo[i].YLocation + height_seat;
                                list.Add(p1);
                                Point p2 = new Point();
                                p2.X = p1.X;
                                p2.Y = p1.Y + height_seat;
                                list.Add(p2);
                                list_lines_seats1.Add(list);
                            }
                            int count_seat_bottom = host_count / 2;
                            int space_seat_bottom = (rewidth - width_seat * count_seat_bottom) / (count_seat_bottom + 1);
                            for (int j = 0; j < count_seat_bottom; j++)
                            {
                                hostInfo[j + count_seat_top].XLocation = space_seat_bottom + j * (width_seat + space_seat_bottom);
                                hostInfo[j + count_seat_top].YLocation = reheight  / 5 + height_seat;

                                List<Point> list = new List<Point>();
                                Point p1 = new Point();
                                p1.X = hostInfo[j + count_seat_top].XLocation + width_seat / 2;
                                p1.Y = hostInfo[j + count_seat_top].YLocation;
                                
                                Point p2 = new Point();
                                p2.X = p1.X;
                                p2.Y = p1.Y - height_seat;
                                list.Add(p2);
                                list.Add(p1);
                                list_lines_seats1.Add(list);
                            }

                            List<Point> list1 = new List<Point>();
                            Point p11 = new Point();
                            p11.X = hostInfo[0].XLocation + width_seat / 2;
                            p11.Y = hostInfo[0].YLocation + height_seat * 2;
                            list1.Add(p11);
                            Point p22 = new Point();
                            p22.X = hostInfo[count_seat_top - 1].XLocation + width_seat / 2;
                            p22.Y = p11.Y;
                            list1.Add(p22);
                            list_lines_seats1.Add(list1);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
           
        }

        /** 设置服务器位置 同时获取服务器之间连线（只有一台交换机） 
         */ 
        private void SetServerLocation_1()
        {
            try
            {
                int server_count = MainForm.serverInfo.Count;
                if (server_count > 0 && server_count <= 10)
                {
                    int space_server = (rewidth - width_server * server_count) / (server_count + 1);
                    for (int i = 0; i < server_count; i++)
                    {
                        serverInfo[i].XLocation = space_server + i * (width_server + space_server);
                        serverInfo[i].YLocation = reheight * 4 / 5 - height_server;
                       
                        List<Point> list = new List<Point>();
                        Point p1 = new Point();
                        p1.X = serverInfo[i].XLocation + width_server / 2;
                        p1.Y = serverInfo[i].YLocation ;
                        list.Add(p1);
                        Point p2 = new Point();
                        p2.X = p1.X;
                        p2.Y = p1.Y - height_server;
                        list.Add(p2);
                        list_lines_servers1.Add(list);
                    }

                    List<Point> list1 = new List<Point>();
                    Point p11 = new Point();
                    p11.X = serverInfo[0].XLocation + width_server / 2;
               //   p11.Y = serverInfo[0].YLocation + height_server * 2;
                    p11.Y = serverInfo[0].YLocation - height_server;      //lxl 20147-9-25-9:54
                    list1.Add(p11);
                    Point p22 = new Point();
                    p22.X = serverInfo[server_count - 1].XLocation + width_server / 2;
                    p22.Y = p11.Y;
                    list1.Add(p22);
                    list_lines_servers1.Add(list1);
                }
                else if (server_count > 10)
                {
                    if (server_count % 2 == 1)
                    {
                        int count_server_top = 0;
                        if (((server_count - 1) / 2) % 2 == 1)
                        {
                            count_server_top = (server_count + 1) / 2;
                        }
                        else
                        {
                            count_server_top = (server_count - 1) / 2;
                        }
                        
                        int space_server_top = (rewidth - width_server * count_server_top) / (count_server_top + 1);
                        for (int i = 0; i < count_server_top; i++)
                        {
                            serverInfo[i].XLocation = space_server_top + i * (width_server + space_server_top);
                            serverInfo[i].YLocation = reheight * 4 / 5 - height_server * 2;

                            List<Point> list = new List<Point>();
                            Point p1 = new Point();
                            p1.X = serverInfo[i].XLocation + width_server / 2;
                            p1.Y = serverInfo[i].YLocation + height_server;
                            list.Add(p1);
                            Point p2 = new Point();
                            p2.X = p1.X;
                            p2.Y = p1.Y + height_server;
                            list.Add(p2);
                            list_lines_servers1.Add(list);
                        }
                        int count_server_bottom = server_count - count_server_top;
                        int space_server_bottom = (rewidth - width_server * count_server_bottom) / (count_server_bottom + 1);
                        for (int j = 0; j < count_server_bottom; j++)
                        {
                            serverInfo[j + count_server_top].XLocation = space_server_bottom + j * (width_server + space_server_bottom);
                            serverInfo[j + count_server_top].YLocation = reheight * 4 / 5 + height_server;

                            List<Point> list = new List<Point>();
                            Point p1 = new Point();
                            p1.X = serverInfo[j + count_server_top].XLocation + width_server / 2;
                            p1.Y = serverInfo[j + count_server_top].YLocation;
                            
                            Point p2 = new Point();
                            p2.X = p1.X;
                            p2.Y = p1.Y - height_server;
                            list.Add(p2);
                            list.Add(p1);
                            list_lines_servers1.Add(list);
                        }


                        if (count_server_top > count_server_bottom)
                        {
                            List<Point> list1 = new List<Point>();
                            Point p11 = new Point();
                            p11.X = serverInfo[0].XLocation + width_server / 2;
                            p11.Y = serverInfo[0].YLocation + height_server * 2;
                            list1.Add(p11);
                            Point p22 = new Point();
                            p22.X = serverInfo[count_server_top - 1].XLocation + width_server / 2;
                            p22.Y = p11.Y;
                            list1.Add(p22);
                            list_lines_servers1.Add(list1);
                        }
                        else 
                        {
                            List<Point> list1 = new List<Point>();
                            Point p11 = new Point();
                            p11.X = serverInfo[count_server_top].XLocation + width_server / 2;
                            p11.Y = serverInfo[count_server_top].YLocation - height_server ;
                            list1.Add(p11);
                            Point p22 = new Point();
                            p22.X = serverInfo[server_count - 1].XLocation + width_server / 2;
                            p22.Y = p11.Y;
                            list1.Add(p22);
                            list_lines_servers1.Add(list1);
                        }
                       
                    }
                    else
                    {
                        if ((server_count / 2) % 2 == 1)
                        {
                            int count_server_top = server_count / 2 + 1;
                            int space_server_top = (rewidth - width_server * count_server_top) / (count_server_top + 1);
                            for (int i = 0; i < count_server_top; i++)
                            {
                                serverInfo[i].XLocation = space_server_top + i * (width_server + space_server_top);
                                serverInfo[i].YLocation = reheight * 4 / 5 - height_server * 2;

                                List<Point> list = new List<Point>();
                                Point p1 = new Point();
                                p1.X = serverInfo[i].XLocation + width_server / 2;
                                p1.Y = serverInfo[i].YLocation + height_server;
                                list.Add(p1);
                                Point p2 = new Point();
                                p2.X = p1.X;
                                p2.Y = p1.Y + height_server;
                                list.Add(p2);
                                list_lines_servers1.Add(list);
                            }
                            int count_server_bottom = server_count / 2 - 1;
                            int space_server_bottom = (rewidth - width_server * count_server_bottom) / (count_server_bottom + 1);
                            for (int j = 0; j < count_server_bottom; j++)
                            {
                                serverInfo[j + count_server_top].XLocation = space_server_bottom + j * (width_server + space_server_bottom);
                                serverInfo[j + count_server_top].YLocation = reheight * 4 / 5 + height_server;

                                List<Point> list = new List<Point>();
                                Point p1 = new Point();
                                p1.X = serverInfo[j + count_server_top].XLocation + width_server / 2;
                                p1.Y = serverInfo[j + count_server_top].YLocation;
                                
                                Point p2 = new Point();
                                p2.X = p1.X;
                                p2.Y = p1.Y - height_server;
                                list.Add(p2);
                                list.Add(p1);
                                list_lines_servers1.Add(list);
                            }

                            List<Point> list1 = new List<Point>();
                            Point p11 = new Point();
                            p11.X = serverInfo[0].XLocation + width_server / 2;
                            p11.Y = serverInfo[0].YLocation + height_server * 2;
                            list1.Add(p11);
                            Point p22 = new Point();
                            p22.X = serverInfo[count_server_top - 1].XLocation + width_server / 2;
                            p22.Y = p11.Y;
                            list1.Add(p22);
                            list_lines_servers1.Add(list1);
                        }
                        else
                        {
                            int count_server_top = server_count / 2;
                            int space_server_top = (rewidth - width_server * count_server_top) / (count_server_top + 1);
                            for (int i = 0; i < count_server_top; i++)
                            {
                                serverInfo[i].XLocation = space_server_top + i * (width_server + space_server_top);
                                serverInfo[i].YLocation = reheight * 4 / 5 - height_server * 2;

                                List<Point> list = new List<Point>();
                                Point p1 = new Point();
                                p1.X = serverInfo[i].XLocation + width_server / 2;
                                p1.Y = serverInfo[i].YLocation + height_server;
                                list.Add(p1);
                                Point p2 = new Point();
                                p2.X = p1.X;
                                p2.Y = p1.Y + height_server;
                                list.Add(p2);
                                list_lines_servers1.Add(list);
                            }
                            int count_server_bottom = server_count / 2;
                            int space_server_bottom = (rewidth - width_server * count_server_bottom) / (count_server_bottom + 1);
                            for (int j = 0; j < count_server_bottom; j++)
                            {
                                serverInfo[j + count_server_top].XLocation = space_server_bottom + j * (width_server + space_server_bottom);
                                serverInfo[j + count_server_top].YLocation = reheight * 4 / 5 + height_server;

                                List<Point> list = new List<Point>();
                                Point p1 = new Point();
                                p1.X = serverInfo[j + count_server_top].XLocation + width_server / 2;
                                p1.Y = serverInfo[j + count_server_top].YLocation;
                                
                                Point p2 = new Point();
                                p2.X = p1.X;
                                p2.Y = p1.Y - height_server;
                                list.Add(p2);
                                list.Add(p1);
                                list_lines_servers1.Add(list);
                            }

                            List<Point> list1 = new List<Point>();
                            Point p11 = new Point();
                            p11.X = serverInfo[0].XLocation + width_server / 2;
                            p11.Y = serverInfo[0].YLocation + height_server * 2;
                            list1.Add(p11);
                            Point p22 = new Point();
                            p22.X = serverInfo[count_server_top - 1].XLocation + width_server / 2;
                            p22.Y = p11.Y;
                            list1.Add(p22);
                            list_lines_servers1.Add(list1);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
            
        }

        /* 根据交换机位置设置席位机位置 同时获取席位机之间连线
         */
        private void SetHostLocation_2(int id, int x) 
        {
            try 
            {
                List<HostInfo> host = new List<HostInfo>();
                List<List<Point>> list_p = new List<List<Point>>();
                string sql = "SELECT * FROM T_ROUTER WHERE ROUTER_CONNPORT = 2 and  ROUTER_SERVERID =  " + id;
                Database db = new Database();
                DataSet ds = db.GetDataSet(sql);
                if (ds != null && ds.Tables.Count > 0) 
                {
                    int count_host = ds.Tables[0].Rows.Count;
                    if(count_host>0)
                    {
                        /*交换机连接席位机个数小于等于6（两个一行）席位机左右间隔暂定1席位机宽度
                         */
                        if (count_host <= 6)
                        {
                            #region 交换机连接奇数台席位机
                            if (count_host % 2 == 1)
                            {
                                int row_count = (count_host + 1) / 2;//行数
                                for (int j = 0; j < count_host; j++)
                                {
                                    for (int i = 0; i < hostInfo.Count; i++)
                                    {
                                        if (hostInfo[i].clientID == int.Parse(ds.Tables[0].Rows[j]["ROUTER_CONNID"].ToString()))
                                        {
                                            if (j == count_host - 1)//奇数个席位机的最顶部一个放置在中间位置
                                            {
                                                hostInfo[i].XLocation = x - width_seat / 2;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - row_count * height_seat * 2;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = x;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = x;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                Point p3 = new Point();
                                                p3.X = p2.X;
                                                p3.Y = p2.Y;
                                                list.Add(p3);
                                                Point p4 = new Point();
                                                p4.X = x;
                                                p4.Y = p2.Y + height_seat * 2;
                                                list.Add(p4);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else //奇数个席位时，除顶部之外的席位位置设置
                                            {
                                                if (j % 2 == 0)//当席位位于左边时
                                                {
                                                    int row = j / 2;//席位机所处行数
                                                    hostInfo[i].XLocation = x - width_seat * 3 / 2;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    Point p3 = new Point();
                                                    p3.X = p2.X;
                                                    p3.Y = p2.Y;
                                                    list.Add(p3);
                                                    Point p4 = new Point();
                                                    p4.X = x;
                                                    p4.Y = p2.Y;
                                                    list.Add(p4);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else//当席位位于右边时
                                                {
                                                    int row = (j - 1) / 2;//席位机所处行数
                                                    hostInfo[i].XLocation = x + width_seat / 2;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    Point p3 = new Point();
                                                    p3.X = p2.X;
                                                    p3.Y = p2.Y;
                                                    Point p4 = new Point();
                                                    p4.X = x;
                                                    p4.Y = p2.Y;
                                                    list.Add(p4);
                                                    list.Add(p3);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                /*每行水平连接线
                                */
                                for (int k = 0; k < row_count - 1; k++)
                                {
                                    List<Point> list = new List<Point>();
                                    Point p1 = new Point();
                                    p1.X = x - width_seat ;
                                    p1.Y = reheight * 5 / 10 - height_seat / 2 - k * 2 * height_seat;
                                    list.Add(p1);
                                    Point p2 = new Point();
                                    p2.X = x + width_seat ;
                                    p2.Y = p1.Y;
                                    list.Add(p2);
                                    list_p.Add(list);
                                }

                                /*竖直连接线
                                 */
                                List<Point> list1 = new List<Point>();
                                Point p11 = new Point();
                                p11.X = x;
                                p11.Y = reheight * 5 / 10 - (row_count - 1) * 2 * height_seat - height_seat / 2;
                                list1.Add(p11);
                                Point p22 = new Point();
                                p22.X = x;
                                p22.Y = reheight * 5 / 10;
                                list1.Add(p22);
                                Point p33 = new Point();
                                p33.X = p22.X;
                                p33.Y = p22.Y;
                                list1.Add(p33);
                                Point p44 = new Point();
                                p44.X = p22.X;
                                p44.Y = p22.Y;
                                list1.Add(p44);
                                list_p.Add(list1);
                            }
                            #endregion 

                            #region 交换机连接偶数台席位机
                            else if (count_host % 2 == 0)
                            {
                                int row_count = count_host / 2;//行数
                                for (int j = 0; j < count_host; j++)
                                {
                                    for (int i = 0; i < hostInfo.Count; i++)
                                    {
                                        if (hostInfo[i].clientID == int.Parse(ds.Tables[0].Rows[j]["ROUTER_CONNID"].ToString()))
                                        {
                                            if (j % 2 == 0)//当席位位于左边时
                                            {
                                                int row = j / 2 ;//席位机所处行数
                                                hostInfo[i].XLocation = x - width_seat * 3 / 2;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                Point p3 = new Point();
                                                p3.X = p2.X;
                                                p3.Y = p2.Y;
                                                list.Add(p3);
                                                Point p4 = new Point();
                                                p4.X = x;
                                                p4.Y = p2.Y;
                                                list.Add(p4);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else//当席位位于右边时
                                            {
                                                int row = (j - 1) / 2;
                                                hostInfo[i].XLocation = x + width_seat / 2;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                Point p3 = new Point();
                                                p3.X = p2.X;
                                                p3.Y = p2.Y;
                                                Point p4 = new Point();
                                                p4.X = x;
                                                p4.Y = p2.Y;
                                                list.Add(p4);
                                                list.Add(p3);
                                                list_p.Add(list);
                                                break;
                                            }
                                            
                                        }
                                    }
                                }

                                /*每行连接所有席位机的横线
                                 */
                                for (int k = 0; k < row_count; k++)
                                {
                                    List<Point> list = new List<Point>();
                                    Point p1 = new Point();
                                    p1.X = x - width_seat ;
                                    p1.Y = reheight * 5 / 10 - height_seat / 2 - k * 2 * height_seat;
                                    list.Add(p1);
                                    Point p2 = new Point();
                                    p2.X = x + width_seat ;
                                    p2.Y = p1.Y;
                                    list.Add(p2);
                                    list_p.Add(list);
                                }

                                List<Point> list1 = new List<Point>();
                                Point p11 = new Point();
                                p11.X = x;
                                p11.Y = reheight * 5 / 10 - (row_count-1) * 2 * height_seat - height_seat / 2;
                                list1.Add(p11);
                                Point p22 = new Point();
                                p22.X = x;
                                p22.Y = reheight * 5 / 10;
                                list1.Add(p22);
                                Point p33 = new Point();
                                p33.X = p22.X;
                                p33.Y = p22.Y;
                                list1.Add(p33);
                                Point p44 = new Point();
                                p44.X = p22.X;
                                p44.Y = p22.Y;
                                list1.Add(p44);
                                list_p.Add(list1);
                            }
                            #endregion
                            
                        }
                        /*交换机连接席位机个数超过6个（四个一行）席位机间隔暂定1/2席位机宽度
                         */
                        else
                        {
                            #region 席位机个数：整行*4+1
                            if (count_host % 4 == 1) 
                            {
                                int row_count = (count_host + 3) / 4;//行数
                                for (int j = 0; j < count_host; j++)
                                {
                                    for (int i = 0; i < hostInfo.Count; i++)
                                    {
                                        if (hostInfo[i].clientID == int.Parse(ds.Tables[0].Rows[j]["ROUTER_CONNID"].ToString()))
                                        {
                                            if (j == count_host - 1)//最顶部一个放置在中间位置
                                            {
                                                hostInfo[i].XLocation = x - width_seat / 2;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - row_count * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = x;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = x;
                                                p2.Y = p1.Y + height_seat * 5 / 2;
                                                list.Add(p2);
                                                list_p.Add(list);

                                                break;
                                            }
                                            else //整行席位位置设置
                                            {
                                                if (j % 4 == 0)//当席位位于左边第一个时
                                                {
                                                    int row = j / 4 ;//席位机所处行数
                                                    hostInfo[i].XLocation = x - width_seat * 11 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 1)//当席位位于左边第二个时
                                                {
                                                    int row = (j - 1) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x - width_seat * 5 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 2)//当席位位于左边第三个时
                                                {
                                                    int row = (j - 2) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x + width_seat / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 3)//当席位位于左边第四个时
                                                {
                                                    int row = (j - 3) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x + width_seat * 7 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                /*每行水平连接线
                                 */
                                for (int k = 0; k < row_count-1;k++ )
                                {
                                    List<Point> list = new List<Point>();
                                    Point p1 = new Point();
                                    p1.X = x - width_seat * 9 / 4;
                                    p1.Y = reheight * 5 / 10 - height_seat / 2 - k * 2 * height_seat;
                                    list.Add(p1);
                                    Point p2 = new Point();
                                    p2.X = x + width_seat * 9 / 4; 
                                    p2.Y = p1.Y ;
                                    list.Add(p2);
                                    list_p.Add(list);
                                }
                                /*竖直连接线
                                 */ 
                                List<Point> list1 = new List<Point>();
                                Point p11 = new Point();
                                p11.X = x ;
                                p11.Y = reheight * 5 / 10 - height_seat / 2 - (row_count-1)* 2 * height_seat;
                                list1.Add(p11);
                                Point p22 = new Point();
                                p22.X = x;
                                p22.Y = reheight * 5 / 10;
                                list1.Add(p22);
                                list_p.Add(list1);
                            }
                            #endregion

                            #region 席位机个数：整行*4+2
                            else if (count_host % 4 == 2)
                            {
                                int row_count = (count_host + 2) / 4;//行数
                                for (int j = 0; j < count_host; j++)
                                {
                                    for (int i = 0; i < hostInfo.Count; i++)
                                    {
                                        if (hostInfo[i].clientID == int.Parse(ds.Tables[0].Rows[j]["ROUTER_CONNID"].ToString()))
                                        {
                                            if (j == count_host - 1)//最后一个放置在中间位置右侧
                                            {
                                                hostInfo[i].XLocation = x + width_seat / 4;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - row_count * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else if (j == count_host - 2)//倒数第二个放置在中间位置左侧
                                            {
                                                hostInfo[i].XLocation = x - width_seat * 5 / 4;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - row_count * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else //整行席位位置设置
                                            {
                                                if (j % 4 == 0)//当席位位于左边第一个时
                                                {
                                                    int row = j / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x - width_seat * 11 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row+1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 1)//当席位位于左边第二个时
                                                {
                                                    int row = (j - 1) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x - width_seat * 5 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 2)//当席位位于左边第三个时
                                                {
                                                    int row = (j - 2) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x + width_seat / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 3)//当席位位于左边第四个时
                                                {
                                                    int row = (j - 3) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x + width_seat * 7 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                            }

                                            
                                        }
                                    }
                                }
                                /*每行水平连接线（除顶层）
                                 */
                                for (int k = 0; k < row_count - 1; k++)
                                {
                                    List<Point> list = new List<Point>();
                                    Point p1 = new Point();
                                    p1.X = x - width_seat * 9 / 4;
                                    p1.Y = reheight * 5 / 10 - height_seat / 2 - k * 2 * height_seat;
                                    list.Add(p1);
                                    Point p2 = new Point();
                                    p2.X = x + width_seat * 9 / 4;
                                    p2.Y = p1.Y ;
                                    list.Add(p2);
                                    list_p.Add(list);
                                }

                                /*顶层水平连接线
                                 */
                                List<Point> list1 = new List<Point>();
                                Point p11 = new Point();
                                p11.X = x - width_seat * 3 / 4;
                                p11.Y = reheight * 5 / 10 - height_seat / 2 - (row_count-1) * 2 * height_seat;
                                list1.Add(p11);
                                Point p22 = new Point();
                                p22.X = x + width_seat * 3 / 4;
                                p22.Y = p11.Y ;
                                list1.Add(p22);
                                list_p.Add(list1);

                                /*竖直连接线
                                */
                                List<Point> list2 = new List<Point>();
                                Point p12 = new Point();
                                p12.X = x ;
                                p12.Y = reheight * 5 / 10 - height_seat / 2 - (row_count - 1) * 2 * height_seat;
                                list2.Add(p12);
                                Point p21 = new Point();
                                p21.X = x ;
                                p21.Y = reheight * 5 / 10;
                                list2.Add(p21);
                                list_p.Add(list2);
                            }
                            #endregion

                            #region 席位机个数：整行*4+3
                            else if (count_host % 4 == 3)
                            {
                                int row_count = (count_host + 1) / 4;//行数
                                for (int j = 0; j < count_host; j++)
                                {
                                    for (int i = 0; i < hostInfo.Count; i++)
                                    {
                                        if (hostInfo[i].clientID == int.Parse(ds.Tables[0].Rows[j]["ROUTER_CONNID"].ToString()))
                                        {
                                            if (j == count_host - 1)//最后一个放置在中间位置右侧
                                            {
                                                hostInfo[i].XLocation = x + width_seat;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - row_count * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else if (j == count_host - 2)//倒数第二个放置在中间位置
                                            {
                                                hostInfo[i].XLocation = x - width_seat / 2;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - row_count * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else if (j == count_host - 3)//倒数第三个放置在中间位置左侧
                                            {
                                                hostInfo[i].XLocation = x - width_seat * 2;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - row_count * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else //整行席位位置设置
                                            {
                                                if (j % 4 == 0)//当席位位于左边第一个时
                                                {
                                                    int row = j / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x - width_seat * 11 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 1)//当席位位于左边第二个时
                                                {
                                                    int row = (j - 1) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x - width_seat * 5 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 2)//当席位位于左边第三个时
                                                {
                                                    int row = (j - 2) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x + width_seat / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10  - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                                else if (j % 4 == 3)//当席位位于左边第四个时
                                                {
                                                    int row = (j - 3) / 4;//席位机所处行数
                                                    hostInfo[i].XLocation = x + width_seat * 7 / 4;
                                                    hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                    host.Add(hostInfo[i]);

                                                    List<Point> list = new List<Point>();
                                                    Point p1 = new Point();
                                                    p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                    p1.Y = hostInfo[i].YLocation + height_seat;
                                                    list.Add(p1);
                                                    Point p2 = new Point();
                                                    p2.X = p1.X;
                                                    p2.Y = p1.Y + height_seat / 2;
                                                    list.Add(p2);
                                                    list_p.Add(list);
                                                    break;
                                                }
                                            }
                                            
                                        }
                                    }
                                }
                                /*每行连接所有席位机的横线
                                 */
                                for (int k = 0; k < row_count - 1; k++)
                                {
                                    List<Point> list = new List<Point>();
                                    Point p1 = new Point();
                                    p1.X = x - width_seat * 9 / 4;
                                    p1.Y = reheight * 5 / 10 - height_seat / 2 - k * 2 * height_seat;
                                    list.Add(p1);
                                    Point p2 = new Point();
                                    p2.X = x + width_seat * 9 / 4;
                                    p2.Y = p1.Y ;
                                    list.Add(p2);
                                    list_p.Add(list);
                                }


                                /*顶层水平连接线
                                 */
                                List<Point> list1 = new List<Point>();
                                Point p11 = new Point();
                                p11.X = x - width_seat * 3 / 2;
                                p11.Y = reheight * 5 / 10 - height_seat / 2 - (row_count - 1) * 2 * height_seat;
                                list1.Add(p11);
                                Point p22 = new Point();
                                p22.X = x + width_seat * 3 / 2;
                                p22.Y = p11.Y;
                                list1.Add(p22);
                                list_p.Add(list1);

                                /*竖直连接线
                                */
                                List<Point> list2 = new List<Point>();
                                Point p12 = new Point();
                                p12.X = x;
                                p12.Y = reheight * 5 / 10 - height_seat / 2 - (row_count - 1) * 2 * height_seat;
                                list2.Add(p12);
                                Point p21 = new Point();
                                p21.X = x;
                                p21.Y = reheight * 5 / 10;
                                list2.Add(p21);
                                list_p.Add(list2);
                            }
                            #endregion

                            #region 席位机个数：整行*4
                            else if (count_host % 4 == 0)
                            {
                                int row_count = (count_host ) / 4;//行数
                                for (int j = 0; j < count_host; j++)
                                {
                                    for (int i = 0; i < hostInfo.Count; i++)
                                    {
                                        if (hostInfo[i].clientID == int.Parse(ds.Tables[0].Rows[j]["ROUTER_CONNID"].ToString()))
                                        {
                                            if (j % 4 == 0)//当席位位于左边第一个时
                                            {
                                                int row = j / 4;//席位机所处行数
                                                hostInfo[i].XLocation = x - width_seat * 11 / 4;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else if (j % 4 == 1)//当席位位于左边第二个时
                                            {
                                                int row = (j - 1) / 4;//席位机所处行数
                                                hostInfo[i].XLocation = x - width_seat * 5 / 4;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                list.Add(p1);
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else if (j % 4 == 2)//当席位位于左边第三个时
                                            {
                                                int row = (j - 2) / 4;//席位机所处行数
                                                hostInfo[i].XLocation = x + width_seat / 4;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p1);
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                            else if (j % 4 == 3)//当席位位于左边第四个时
                                            {
                                                int row = (j - 3) / 4;//席位机所处行数
                                                hostInfo[i].XLocation = x + width_seat * 7 / 4;
                                                hostInfo[i].YLocation = reheight * 5 / 10 - (row + 1) * 2 * height_seat;
                                                host.Add(hostInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = hostInfo[i].XLocation + width_seat / 2;
                                                p1.Y = hostInfo[i].YLocation + height_seat;
                                                Point p2 = new Point();
                                                p2.X = p1.X;
                                                p2.Y = p1.Y + height_seat / 2;
                                                list.Add(p1);
                                                list.Add(p2);
                                                list_p.Add(list);
                                                break;
                                            }
                                           
                                        }
                                    }
                                }
                                /*每行连接所有席位机的横线
                                 */
                                for (int k = 0; k < row_count; k++)
                                {
                                    List<Point> list = new List<Point>();
                                    Point p1 = new Point();
                                    p1.X = x - width_seat * 9 / 4;
                                    p1.Y = reheight * 5 / 10 - height_seat / 2 - k * 2 * height_seat;
                                    list.Add(p1);
                                    Point p2 = new Point();
                                    p2.X = x + width_seat * 9 / 4;
                                    p2.Y = p1.Y ;
                                    list.Add(p2);
                                    list_p.Add(list);
                                }

                                /*竖直连接线
                                 */
                                List<Point> list2 = new List<Point>();
                                Point p12 = new Point();
                                p12.X = x;
                                p12.Y = reheight * 5 / 10 - height_seat / 2 - (row_count-1) * 2 * height_seat;
                                list2.Add(p12);
                                Point p21 = new Point();
                                p21.X = x;
                                p21.Y = reheight * 5 / 10;
                                list2.Add(p21);
                                list_p.Add(list2);
                            }
                            #endregion

                        }
                        
                    }
                    
                }
                list_lines_seats2.Add(list_p);
                hostlist.Add(host);
            }
            catch(Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        /* 根据交换机位置设置服务器位置 同时获取服务器之间连线
         */ 
        private void SetServerLocation_2(int id, int x)
        {
            try
            {
                List<HostInfo> server = new List<HostInfo>();
                List<List<Point>> list_line = new List<List<Point>>();
                string sql = "SELECT * FROM T_ROUTER WHERE ROUTER_CONNPORT = 1 and  ROUTER_SERVERID =  " + id;
                Database db = new Database();
                DataSet ds = db.GetDataSet(sql);
                if (ds != null && ds.Tables.Count > 0)
                {
                    int count_server = ds.Tables[0].Rows.Count;
                    if (count_server > 0)
                    {
                        //服务器间距离与上边席位机对齐，选用1席位机宽度
                        if (count_server % 2 == 1) //交换机连接奇数台服务器
                        {
                            int row_count = (count_server + 1) / 2;//行数
                            for (int j = 0; j < count_server; j++)
                            {
                                for (int i = 0; i < serverInfo.Count; i++)
                                {
                                    if (serverInfo[i].clientID == int.Parse(ds.Tables[0].Rows[j]["ROUTER_CONNID"].ToString()))
                                    {
                                        if (j == count_server - 1)//奇数个的最顶部一个放置在中间位置
                                        {
                                            serverInfo[i].XLocation = x - width_server / 2;
                                            serverInfo[i].YLocation = reheight * 7 / 10 + height_server
                                                + (row_count - 1) * height_server * 5 / 3;
                                            server.Add(serverInfo[i]);

                                            List<Point> list = new List<Point>();
                                            Point p1 = new Point();
                                            p1.X = x;
                                            p1.Y = serverInfo[i].YLocation;
                                            Point p2 = new Point();
                                            p2.X = x;
                                            p2.Y = serverInfo[i].YLocation - height_server * 3 / 2;
                                            list.Add(p2);
                                            list.Add(p1);
                                            list_line.Add(list);
                                            break;
                                        }
                                        else //奇数个服务器时，除顶部之外的席位位置设置
                                        {
                                            if (j % 2 == 0)//当服务器位于左边时
                                            {
                                                int row = j / 2;//服务器所处行数
                                                serverInfo[i].XLocation = x - width_seat / 2 - width_server;
                                                serverInfo[i].YLocation = reheight * 7 / 10 + height_server
                                                    + row * height_server * 5 / 3;
                                                server.Add(serverInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = x - width_seat / 2;
                                                p1.Y = serverInfo[i].YLocation + height_server / 2;
                                                Point p2 = new Point();
                                                p2.X = x;
                                                p2.Y = p1.Y;
                                                list.Add(p1);
                                                list.Add(p2);
                                                list_line.Add(list);
                                                break;
                                            }
                                            else//当服务器位于右边时
                                            {
                                                int row = (j - 1) / 2;//服务器所处行数
                                                serverInfo[i].XLocation = x + width_seat / 2;
                                                serverInfo[i].YLocation = reheight * 7 / 10 + height_server
                                                    + row * height_server * 5 / 3;
                                                server.Add(serverInfo[i]);

                                                List<Point> list = new List<Point>();
                                                Point p1 = new Point();
                                                p1.X = x + width_seat / 2;
                                                p1.Y = serverInfo[i].YLocation + height_server / 2;
                                                Point p2 = new Point();
                                                p2.X = x;
                                                p2.Y = p1.Y;
                                                list.Add(p2);
                                                list.Add(p1);
                                                list_line.Add(list);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            /*竖直连接线
                                 */
                            List<Point> list2 = new List<Point>();
                            Point p12 = new Point();
                            p12.X = x;
                            p12.Y = reheight * 7 / 10 ;
                            list2.Add(p12);
                            Point p21 = new Point();
                            p21.X = x;
                            p21.Y = reheight * 7 / 10  + (row_count - 1) * height_server * 5 / 3;//- height_server / 2
                            list2.Add(p21);
                            list_line.Add(list2);
                        }
                        else if (count_server % 2 == 0)//交换机连接偶数台服务器
                        {
                            int row_count = count_server / 2;//行数
                            for (int j = 0; j < count_server; j++)
                            {
                                for (int i = 0; i < serverInfo.Count; i++)
                                {
                                    if (serverInfo[i].clientID == int.Parse(ds.Tables[0].Rows[j]["ROUTER_CONNID"].ToString()))
                                    {
                                        if (j % 2 == 0)//当服务器位于左边时
                                        {
                                            int row = j / 2;//服务器所处行数
                                            serverInfo[i].XLocation = x - width_seat / 2 - width_server;
                                            serverInfo[i].YLocation = reheight * 7 / 10 + height_server
                                                + row * height_server * 5 / 3;
                                            server.Add(serverInfo[i]);

                                            List<Point> list = new List<Point>();
                                            Point p1 = new Point();
                                            p1.X = x - width_seat / 2;
                                            p1.Y = serverInfo[i].YLocation + height_server / 2;
                                            Point p2 = new Point();
                                            p2.X = x;
                                            p2.Y = p1.Y;
                                            list.Add(p1);
                                            list.Add(p2);
                                            list_line.Add(list);
                                            break;
                                        }
                                        else//当服务器位于右边时
                                        {
                                            int row = (j - 1) / 2;
                                            serverInfo[i].XLocation = x + width_seat / 2;
                                            serverInfo[i].YLocation = reheight * 7 / 10 + height_server
                                                 + row * height_server * 5 / 3;
                                            server.Add(serverInfo[i]);

                                            List<Point> list = new List<Point>();
                                            Point p1 = new Point();
                                            p1.X = x + width_seat / 2;
                                            p1.Y = serverInfo[i].YLocation + height_server / 2;
                                            Point p2 = new Point();
                                            p2.X = x;
                                            p2.Y = p1.Y;
                                            list.Add(p2);
                                            list.Add(p1);
                                            list_line.Add(list);
                                            break;
                                        }
                                    }
                                }
                            }
                            /*竖直连接线
                                 */
                            List<Point> list2 = new List<Point>();
                            Point p12 = new Point();
                            p12.X = x;
                            p12.Y = reheight * 7 / 10 ;
                            list2.Add(p12);
                            Point p21 = new Point();
                            p21.X = x;
                            p21.Y = reheight * 7 / 10 + row_count * height_server * 5 / 3;//- height_server / 2
                            list2.Add(p21);
                            list_line.Add(list2);
                            
                        }
                    }
                }
                list_lines_servers2.Add(list_line);
                serverlist.Add(server);
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        /*清空信息列表
         */
        private void listClear()
        {
            try
            {
                switch_collunm = 1;
                switch_row = 1;

                switchlist.Clear();
                hostlist.Clear();
                serverlist.Clear();

                //list_lines_switches.Clear();
                list_lines_seats1.Clear();
                list_lines_servers1.Clear();
                list_lines_seats2.Clear();
                list_lines_servers2.Clear();
            }
            catch (Exception ee)
            {
                MessageBoxEx.Show(ee.ToString());
            }
        }
        #endregion

        #region  绘制设备间连线
        private void DrawLines(Graphics g)
        {
            try
            {
                int switchcount = switchInfo.Count;
                if (switchcount == 1)
                {
                    DrawLine_Seat1(g);
                    DrawLine_Server1(g);
                }
                else 
                {
                    DrawLine_Seat2(g);
                    DrawLine_Server2(g);
                }
                DrawLine_Switch(g);
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        } 

        /*绘制席位机之间的连线(只有一个交换机时)
         */
        private void DrawLine_Seat1(Graphics g) 
        {
            try 
            {
                int len1 = list_lines_seats1.Count;
                int len2 = hostInfo.Count;
                for (int i = 0; i < len1;i++ ) 
                {
                    Point p1 = list_lines_seats1[i][0];
                    Point p2 = list_lines_seats1[i][1];
                    if (i < len2)
                    {
                        if (hostInfo[i].bActive == false)
                        {
                            g.DrawImage(Topology.image_DataLineFalse_v, p1.X-4, p1.Y, 8, height_seat);
                        }
                        else 
                        {
                            g.DrawImage(Topology.image_DataLineTrue_v, p1.X - 4, p1.Y, 8, height_seat);
                        }
                    }
                    else 
                    {
                        if (p1.Y == p2.Y)
                        {
                            int width_im = p2.X - p1.X;
                            g.DrawImage(Topology.imgh, p1.X, p1.Y - 4, width_im, 8);//水平
                        }
                        else 
                        {
                            int heigt_im = p2.Y - p1.Y;
                            g.DrawImage(Topology.imgv, p1.X - 4, p1.Y, 8, heigt_im);//竖直
                        }
                        
                    }
                }
            }
            catch(Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        /*绘制服务器之间的连线(只有一个交换机时)
         */
        private void DrawLine_Server1(Graphics g)
        {
            try
            {
                int len1 = list_lines_servers1.Count;
                int len2 = serverInfo.Count;
                //如果只有一台服务器，不需要画服务器之间的连接线
                if (len2 == 0)
                {
                    return;
                }
                for (int i = 0; i < len1; i++)
                {
                    Point p1 = list_lines_servers1[i][0];
                    Point p2 = list_lines_servers1[i][1];
                    if (i < len2)
                    {
                        if (serverInfo[i].bActive == false)
                        {
                            g.DrawImage(Topology.image_DataLineFalse_v, p2.X - 4, p2.Y, 8, height_server);
                        }
                        else
                        {
                            g.DrawImage(Topology.image_DataLineTrue_v, p2.X - 4, p2.Y, 8, height_server);
                        }
                    }
                    else
                    {
                        if (p1.Y == p2.Y)
                        {
                            int width_im = p2.X - p1.X+8;
                            g.DrawImage(Topology.imgh, p1.X-4, p1.Y - 4, width_im, 8);//水平
                        }
                        else
                        {
                            int heigt_im = p2.Y - p1.Y;
                            g.DrawImage(Topology.imgv, p1.X - 4, p1.Y, 8, heigt_im);//竖直
                        }

                    }
                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        /*绘制交换机之间的连线(只有一个交换机时)
         */
        private void DrawLine_Switch(Graphics g)
        {
            try
            {
                int count = switchInfo.Count;
                if(count==1)
                {
                    Point p1 = new Point();
                    p1.X = switchInfo[0].XLocation + width_switch / 2;
                    p1.Y = switchInfo[0].YLocation;
                    Point p2 = new Point();
                    p2.X = p1.X;
                    p2.Y = reheight / 5;
                    Point p3 = new Point();
                    p3.X = p1.X;
                    p3.Y = switchInfo[0].YLocation + height_switch;
                    Point p4 = new Point();
                    p4.X = p1.X;
                    p4.Y = reheight * 4 / 5;
                  
                    if(hostInfo.Count>0)
                    {
                        if (hostInfo.Count < 10)
                        {
                            g.DrawImage(Topology.imgv, p2.X - 4, p2.Y + Topology.image_HostBlue.Height, 8, p1.Y - p2.Y - Topology.image_HostBlue.Height);//竖直向上连接席位机
                        }
                        else
                            g.DrawImage(Topology.imgv, p2.X - 4, p2.Y + 4, 8, p1.Y - p2.Y);//竖直向上连接席位机

                    }
                    
                    if(serverInfo.Count>0)
                    {
                        //梁晓磊改
                        int h = serverInfo[0].YLocation;      
                       // g.DrawImage(Topology.imgv, p3.X - 4, p4.Y, 8, p4.Y - p3.Y);//竖直向下连接服务器
                        if(serverInfo.Count ==1)
                            g.DrawImage(Topology.imgv, p3.X - 4, p3.Y, 8, h - p3.Y);//竖直向下连接服务器
                        else
                            g.DrawImage(Topology.imgv, p3.X - 4, p3.Y, 8, h - p3.Y-height_server);//竖直向下连接服务器，应减去一个服务器的高度
                    }
                    
                }
                else if(count>1)
                {
                    for (int i = 0; i < count;i++ )
                    {
                        /*//竖直向上连接席位机
                         */
                        if(list_lines_seats2[i].Count>0)
                        {
                            Point p1 = new Point();
                            p1.X = switchInfo[i].XLocation + width_switch / 2;
                            p1.Y = switchInfo[i].YLocation;

                            Point p2 = new Point();
                            p2.X = p1.X;
                            p2.Y = reheight / 2;

                            g.DrawImage(Topology.imgv, p2.X - 4, p2.Y, 8, p1.Y - p2.Y);//竖直向上连接席位机
                        }
                        /*//竖直向下连接服务器
                         */
                        if (list_lines_servers2[i].Count > 0)
                        {
                            Point p1 = new Point();
                            p1.X = switchInfo[i].XLocation + width_switch / 2;
                            p1.Y = switchInfo[i].YLocation + height_switch;

                            Point p2 = new Point();
                            p2.X = p1.X;
                            p2.Y = reheight * 7 / 10;

                            g.DrawImage(Topology.imgv, p1.X - 4, p1.Y, 8, p2.Y - p1.Y);//竖直向下连接服务器
                        }
                        /*//连接相邻交换机
                         */
                        if (i < count - 1)
                        {
                            if (switchChildCount[i] == 1)
                            {
                                Point p1 = new Point();
                                p1.X = switchlist[i].XLocation + width_switch;
                                p1.Y = switchlist[i].YLocation + height_switch / 2;

                                Point p2 = new Point();
                                p2.X = switchlist[i + 1].XLocation;
                                p2.Y = p1.Y;
                                g.DrawImage(Topology.imgh, p1.X, p1.Y - 5, p2.X - p1.X, 10);
                            }
                            else if (switchChildCount[i] > 1)
                            {

                                Point p1 = new Point();
                                p1.X = switchlist[i].XLocation + width_switch;
                                p1.Y = switchlist[i].YLocation + height_switch / 2;

                                Point p2 = new Point();
                                p2.X = switchlist[i + 1].XLocation;
                                p2.Y = p1.Y;
                                g.DrawImage(Topology.imgh, p1.X, p1.Y - 5, p2.X - p1.X, 10);



                                Point p3 = new Point();
                                p3.X = p1.X;
                                p3.Y = switchlist[i].YLocation + height_switch / 2;

                                Point p4 = new Point();
                                p4.X = p1.X;
                                p4.Y = switchlist[i + switchChildCount[i]].YLocation + height_switch / 2;

                                g.DrawImage(Topology.imgv, p3.X, p3.Y, 10, p4.Y - p3.Y);


                                for (int j = 1; j <= switchChildCount[i]; j++)
                                {
                                    Point p = new Point();
                                    p.X = switchlist[i + j].XLocation;
                                    p.Y = switchlist[i + j].YLocation + height_switch / 2;

                                    g.DrawImage(Topology.imgh, p3.X, p.Y - 5, p.X - p3.X, 10);
                                }
                            }
                        }
                     
                    }
                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        /*绘制席位机之间的连线
         */
        private void DrawLine_Seat2(Graphics g)
        {
            try
            {
                int count_list = list_lines_seats2.Count;
                for (int i = 0; i < count_list;i++ ) 
                {
                    int count_seat = hostlist[i].Count;
                    int count_line = list_lines_seats2[i].Count;

                    #region  席位机个数小于等于6时
                    if (count_seat <= 6)
                    {
                        for (int j = 0; j < count_line; j++)
                        {

                            Point p1 = list_lines_seats2[i][j][0];
                            Point p2 = list_lines_seats2[i][j][1];
                            //Point p3 = list_lines_seats2[i][j][2];
                            //Point p4 = list_lines_seats2[i][j][3];

                            if (j < count_seat)
                            {

                                int len_v = Math.Abs(p2.Y - p1.Y) + 4;
                                //int len_h = Math.Abs(p4.X - p3.X);

                                if (hostlist[i][j].bActive == false)
                                {
                                    //g.DrawImage(Topology.image_DataLineFalse_h, p3.X, p3.Y - 4, len_h, 8);
                                    g.DrawImage(Topology.image_DataLineFalse_v, p1.X - 4, p1.Y, 8, len_v);
                                }
                                else
                                {
                                    //g.DrawImage(Topology.image_DataLineTrue_h, p3.X, p3.Y - 4, len_h, 8);
                                    g.DrawImage(Topology.image_DataLineTrue_v, p1.X - 4, p1.Y, 8, len_v);

                                }
                            }
                            else
                            {
                                if (p1.Y == p2.Y)
                                {
                                    int width_im = p2.X - p1.X+8;
                                    g.DrawImage(Topology.imgh, p1.X-4, p1.Y-4, width_im, 8);//水平
                                }
                                else
                                {
                                    int heigt_im = p2.Y - p1.Y;
                                    g.DrawImage(Topology.imgv, p1.X - 4, p1.Y, 8, heigt_im);//竖直
                                }

                            }
                        }
                    }
                    #endregion 
                    
                    #region  席位机个数大于6时
                    else
                    {
                        for (int j = 0; j < count_line; j++)
                        {

                            Point p1 = list_lines_seats2[i][j][0];
                            Point p2 = list_lines_seats2[i][j][1];

                            if (j < count_seat)
                            {

                                int len_v = Math.Abs(p2.Y - p1.Y) + 4;
                               

                                if (hostlist[i][j].bActive == false)
                                {
                                    g.DrawImage(Topology.image_DataLineFalse_v, p1.X - 4, p1.Y, 8, len_v);
                                }
                                else
                                {
                                    g.DrawImage(Topology.image_DataLineTrue_v, p1.X - 4, p1.Y, 8, len_v);

                                }
                            }
                            else
                            {
                                if (p1.Y == p2.Y)
                                {
                                    int width_im = p2.X - p1.X+8;
                                    g.DrawImage(Topology.imgh, p1.X-4, p1.Y - 4, width_im, 8);//水平
                                }
                                else
                                {
                                    int heigt_im = p2.Y - p1.Y;
                                    g.DrawImage(Topology.imgv, p1.X - 4, p1.Y, 8, heigt_im);//竖直
                                }

                            }
                        }
                    }
                    #endregion
                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        /*绘制服务器之间的连线
         */
        private void DrawLine_Server2(Graphics g)
        {
            try
            {
                int count_list = list_lines_servers2.Count;
                for (int i = 0; i < count_list; i++)
                {
                    int count_server = serverlist[i].Count;
                    int count_line = list_lines_servers2[i].Count;
                    for (int j = 0; j < count_line; j++)
                    {

                        Point p1 = list_lines_servers2[i][j][0];
                        Point p2 = list_lines_servers2[i][j][1];

                        if (j < count_server)
                        {

                            int len_v = Math.Abs(p2.Y - p1.Y);
                            int len_h = Math.Abs(p2.X - p1.X);

                            if (serverlist[i][j].bActive == false)
                            {
                                if (len_v > 0)
                                {
                                    g.DrawImage(Topology.image_DataLineFalse_v, p1.X-4 , p1.Y, 8, len_v);
                                }
                                else if (len_h > 0)
                                {
                                    g.DrawImage(Topology.image_DataLineFalse_h, p1.X, p1.Y-4 , len_h, 8);
                                }
                            }
                            else
                            {
                                if (len_v > 0)
                                {
                                    g.DrawImage(Topology.image_DataLineTrue_v, p1.X-4, p1.Y, 8, len_v);
                                }
                                else if (len_h > 0)
                                {
                                    g.DrawImage(Topology.image_DataLineTrue_h, p1.X, p1.Y-4, len_h, 8);
                                }

                            }
                        }
                        else
                        {
                            if (p1.Y == p2.Y)
                            {
                                int width_im = p2.X - p1.X + 8;
                                g.DrawImage(Topology.imgh, p1.X - 4, p1.Y - 4, width_im, 8);//水平
                            }
                            else
                            {
                                int heigt_im = p2.Y - p1.Y;
                                g.DrawImage(Topology.imgv, p1.X - 4, p1.Y-4, 8, heigt_im);//竖直
                            }

                        }
                    }
                        

                }
            }
            catch (Exception e)
            {
                MessageBoxEx.Show(e.ToString());
            }
        }

        #endregion
        
        #region 画席位名称 服务器名称
       
        private void DrawServerName(Graphics g)
        {
            for (int i = 0; i < serverInfo.Count; i++)
            {
                int servernamelength = DeviceCodeLength(serverInfo[i].seatName) ;
                g.DrawString(serverInfo[i].seatName, MyFont, Brushes.Black, new PointF(serverInfo[i].XLocation + width_server / 2 - servernamelength / 2, serverInfo[i].YLocation + height_server +2));
            }

            for (int i = 0; i < hostInfo.Count;i++ )
            {
                int hostnamelength = DeviceCodeLength(hostInfo[i].seatName) ;
                g.DrawString(hostInfo[i].seatName, MyFont, Brushes.Black, new PointF(hostInfo[i].XLocation + width_seat / 2 - hostnamelength / 2, hostInfo[i].YLocation-12 ));
            }

            for (int i = 0; i < switchInfo.Count; i++)
            {
                int switchnamelength = DeviceCodeLength(switchInfo[i].seatName);
                g.DrawString(switchInfo[i].seatName, MyFont, Brushes.Black, new PointF(switchInfo[i].XLocation + width_switch / 2 +5 , switchInfo[i].YLocation - 12));
            }
        }
        
        #endregion

        #region 内部设备图标双击事件
        /// <summary>
        /// 内部设备图标双击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InnerDevice_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MessageBoxEx.UseSystemLocalizedString = true;
            try
            {
                if (e.Button == MouseButtons.Left && (2 == e.Clicks))
                {
                    for (int i = 0; i < hostInfo.Count; i++)
                    {
                        Rectangle rect = new Rectangle(hostInfo[i].XLocation, hostInfo[i].YLocation, Topology.image_HostBlue.Width, Topology.image_HostBlue.Height);
                        if (rect.Contains(e.X, e.Y))
                        {
                            ClickDevID = hostInfo[i].clientID;
                            if (hostInfoForm == null || hostInfoForm.IsDisposed)
                            {
                                hostInfoForm = new HostInfoForm();
                                hostInfoForm.Owner = this;
                            }
                            hostInfoForm.m_hostIPAddr = hostInfo[i].m_sIPInDatabase;
                            hostInfoForm.Show();
                            hostInfoForm.WindowState = FormWindowState.Normal;
                            hostInfoForm.Activate();
                            goto end;
                        }
                    }

                    for (int i = 0; i < switchInfo.Count; i++)
                    {
                        Rectangle rect = new Rectangle(switchInfo[i].XLocation, switchInfo[i].YLocation, Topology.image_serverBlue.Width, Topology.image_serverBlue.Height);
                        if (rect.Contains(e.X, e.Y))
                        {
                            ClickDevID = switchInfo[i].clientID;
                            if (switchInfoForm == null || switchInfoForm.IsDisposed)
                            {
                                switchInfoForm = new SwitchInfoForm();
                                switchInfoForm.Owner = this;
                            }
                            switchInfoForm.SwitchIP = switchInfo[i].m_sIPInDatabase;
                            switchInfoForm.SwitchID = ClickDevID;
                            switchInfoForm.Show();
                            switchInfoForm.WindowState = FormWindowState.Normal;
                            switchInfoForm.Activate();
                            goto end;
                        }
                    }

                    for (int i = 0; i < serverInfo.Count; i++)
                    {
                        Rectangle rect = new Rectangle(serverInfo[i].XLocation, serverInfo[i].YLocation, Topology.image_serverBlue.Width, Topology.image_serverBlue.Height);
                        if (rect.Contains(e.X, e.Y))
                        {
                            ClickDevID = serverInfo[i].clientID;
                            if (hostInfoForm == null || hostInfoForm.IsDisposed)
                            {
                                hostInfoForm = new HostInfoForm();
                                hostInfoForm.Owner = this;
                            }
                            hostInfoForm.m_hostIPAddr = serverInfo[i].m_sIPInDatabase;
                            hostInfoForm.Show();
                            hostInfoForm.WindowState = FormWindowState.Normal;
                            hostInfoForm.Activate();
                            goto end;
                        }
                    }

                   
                    end: ;
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(ex.ToString());
            }
        }
        #endregion

        #region  服务器鼠标右键单击事件
        /// <summary>
        /// 鼠标右键单击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControlPanel_Inner_MouseClick(object sender, MouseEventArgs e)
        {
            if((1==e.Clicks) && e.Button==MouseButtons.Right)
            {
                for (int i = 0; i < serverInfo.Count;i++ ) 
                {
                    Rectangle rect = new Rectangle(serverInfo[i].XLocation, serverInfo[i].YLocation, Topology.image_serverBlue.Width, Topology.image_serverBlue.Height);
                    if(rect.Contains(e.X,e.Y))
                    {
                        ServerURL = serverInfo[i].url;
                        this.contextMenuStrip_Server.Show(e.X + this.expandPanel_Left.Width, e.Y + this.tabControl_Menu.Height + 50);
                    }
                }
            }
        }
        /// <summary>
        /// 登陆数据库服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem_Server_Click(object sender, EventArgs e)
        {
            if (ServerURL != "" && ServerURL != null)
            {
                Process.Start("IExplore.exe", ServerURL);
            }
            else
            {
                MessageBoxEx.Show("该服务器无法登陆！");
            }
        }
        #endregion

        #region 左侧信息显示列宽度
        /// <summary>
        /// 系统信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewSysInfo_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (this.listViewSysInfo.Columns[e.ColumnIndex].Width<100) 
            {
                this.listViewSysInfo.Columns[e.ColumnIndex].Width = 100;
            }
        }
        /// <summary>
        /// 告警信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewAlarmInfo_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (this.listViewAlarmInfo.Columns[e.ColumnIndex].Width < 100)
            {
                this.listViewAlarmInfo.Columns[e.ColumnIndex].Width = 100;
            }
        }
        /// <summary>
        /// 输出信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewOutputInfo_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (MainForm.listViewOutputInfo.Columns[e.ColumnIndex].Width < 100)
            {
                MainForm.listViewOutputInfo.Columns[e.ColumnIndex].Width = 100;
            }
        }
        #endregion

        private void btnItem_voice_Click(object sender, EventArgs e)
        {
            if (IsVoiceOn)
            {
                IsVoiceOn = false;
                btnItem_voice.Text = "开启声音";
            }
            else
            {
                IsVoiceOn = true;
                btnItem_voice.Text = "关闭声音";
            }
        }
        /// <summary>
        /// write status of all device into log
        /// </summary>
        private void writeStaToLog()
        {
            LogInfo logInfo = new LogInfo();
            int down=0;   //断路的机器数目 
            int warning=0; //未获取代理数据的数目
            int alarm=0;    //告警的数目
            int normal = 0;  //正常的数目
            for (int i = 0; i < hostInfo.Count+serverInfo.Count; i++)
            {
                if (!allInfo[i].bActive)
                {
                    down++;
                }
                else if (!allInfo[i].bNormal)
                {
                    alarm++;
                }
                else if (allInfo[i].lCurRecvLen != 0)
                {
                    normal++;
                }
                else
                {
                    warning++;
                }

            }
            string netstat = "主机数: " + hostInfo.Count + "  服务器数: " + serverInfo.Count + "  正常数: " + normal + "  告警数: " + alarm + "  未知状态: " + (warning + down);
            string devstat = "主机数: " + hostInfo.Count + "  服务器数: " + serverInfo.Count + "  通路数: " + (normal+warning+alarm) + "  断路数: " + down;
            //设备状态日志
            logInfo.logcontent = netstat;
            logInfo.maintype = "状态";
            logInfo.senctype = "设备";
            logInfo.WriteStaLog();
            //网络状态日志
            logInfo.logcontent = devstat;
            logInfo.maintype = "状态";
            logInfo.senctype = "网络";
            logInfo.WriteStaLog();
        }

        private void listViewAlarmInfo_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                ListViewEx it = (ListViewEx)sender;
                ListViewItem items = it.SelectedItems[0];
                string id = items.SubItems[2].Text;
                if (id == string.Empty)
                {
                    return;
                }
                int allid = Convert.ToInt32(items.SubItems[2].Text);
                if (allInfo[allid].type == TYPEOFDEVICE.SERVER || (allInfo[allid].type == TYPEOFDEVICE.HOST))
                {
                    if (hostInfoForm == null || hostInfoForm.IsDisposed)
                    {
                        hostInfoForm = new HostInfoForm();
                        hostInfoForm.Owner = this;
                    }
                    hostInfoForm.m_hostIPAddr = allInfo[allid].m_sIPInDatabase;
                    hostInfoForm.Show();
                    hostInfoForm.WindowState = FormWindowState.Normal;
                    hostInfoForm.Activate();
                }
            }
            catch (Exception)
            {

            }
        }

        private void menustrip_item_clearAlarm_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否清空告警信息列表，被清除的信息可在历史信息中查询", "确认清空", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                this.listViewAlarmInfo.Items.Clear();
            }
            else
            {
                return;
            }
        }

        private void ReadProcess(string filepath)
        {
            if (!File.Exists(filepath))
            {
                createfile(filepath);
                return;
            }
            try
            {
                XDocument xdoc2 = XDocument.Load(filepath);
                for (int id = 0; id < allInfo.Count; id++)
                {
                    var querResult = from c in xdoc2.Descendants("client")
                                     where c.Attribute("name").Value == MainForm.allInfo[id].hostName
                                     select c.Value;
                    foreach (var item in querResult)
                    {
                        string pros = item.ToString();
                        string[] data = item.Split(';');
                        for (int j = 0; j < data.Length; j++)
                        {
                            if(data[j] != "")
                                allInfo[id].forbiddenPro.Add(data[j].Trim(),true);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

/// <summary>
/// 获取systemData文件中存取的各个机器限制使用的进程名
/// </summary>
        private void SetforbiddenPros()
        {
            string filepath = "systemData";
            ReadProcess(filepath);      
        }

        private void createfile(string filepath)
        {
            if (!File.Exists(filepath))
            {
                XDocument xdoc = new XDocument(
                    new XElement("Prcocess"));
                xdoc.Save(filepath);
            }
        }

        private void updatexmlfile(string filepath, string clientname, string proname)
        {
            createfile(filepath);
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);
                XmlElement root = doc.DocumentElement;
                foreach (XmlNode node in root)
                {
                    if (node.Name == "client")
                    {
                        foreach (XmlAttribute xattr in node.Attributes)
                        {
                            if (xattr.Name == "name")
                            {
                                if (xattr.Value == clientname)
                                {
                                    node.InnerText = proname;
                                    doc.Save(filepath);
                                    return;
                                }
                            }
                        }
                    }
                }
                XmlNode node2 = doc.CreateElement("client");
                XmlNode attrTime = doc.CreateNode(XmlNodeType.Attribute, "name", null);
                attrTime.Value = clientname;
                node2.Attributes.SetNamedItem(attrTime);
                node2.InnerText = proname;
                root.AppendChild(node2);
                doc.Save(filepath);
            }
            catch (Exception)
            {
            }
        }


    }
}