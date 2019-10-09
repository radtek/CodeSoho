// MoniClientDlg.cpp : implementation file
//

#include "stdafx.h"
#include "afxcmn.h"
#include <vector>
#include <Psapi.h>
#include "MoniClient.h"
#include "MoniClientDlg.h"
#include "ftpclass.h"
#include <cstringt.h>
#include <atlbase.h>
#include <atlconv.h>
#include <wchar.h>
#include <windows.h>
#include "InitFile.h"
#include "ThreadRecvBroadcastData.h"
#include "iphlpapi.h"

#pragma comment(lib,"Iphlpapi.lib")
typedef long long int64_t;
typedef unsigned long long uint64_t;
#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

CString strLocalIpAddr;				//�󶨱��ص�һ��IP��ַ  //ע:ԭ����������ClientDlg.h�ж����
CLocalInfo m_localInfo;
SOCKET sock;
char* recvbuf;
char mac[20]; //mac��ַ
SOCKET sock2;
char* recvbuf2;

//���̵���
#define  WM_MY_TRAY_NOTIFICATION  WM_USER+100
BOOL TrayMessage(HWND hWnd, DWORD dwMessage, HICON hIcon, PSTR pszTip)
{
	BOOL bReturn;
	NOTIFYICONDATA NotifyData;
	NotifyData.cbSize = sizeof(NOTIFYICONDATA);
	NotifyData.hWnd = hWnd;
	NotifyData.uID = IDI_ICON1;
	NotifyData.uFlags = NIF_MESSAGE|NIF_ICON|NIF_TIP;
	NotifyData.uCallbackMessage = WM_MY_TRAY_NOTIFICATION;
	NotifyData.hIcon = hIcon;
	lstrcpyn(NotifyData.szTip, pszTip, sizeof(NotifyData.szTip));
	bReturn = Shell_NotifyIcon(dwMessage, &NotifyData);
	if (hIcon)
		DestroyIcon(hIcon);
	return bReturn;
}

/////////////////////////////////////////////////////////////////////////////
// CAboutDlg dialog used for App About

class CAboutDlg : public CDialog
{
public:
	CAboutDlg();

// Dialog Data
	//{{AFX_DATA(CAboutDlg)
	enum { IDD = IDD_ABOUTBOX };
	//}}AFX_DATA

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CAboutDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	//{{AFX_MSG(CAboutDlg)
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialog(CAboutDlg::IDD)
{
	//{{AFX_DATA_INIT(CAboutDlg)
	//}}AFX_DATA_INIT
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CAboutDlg)
	//}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
	//{{AFX_MSG_MAP(CAboutDlg)
		// No message handlers
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CMoniClientDlg dialog

CMoniClientDlg::CMoniClientDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CMoniClientDlg::IDD, pParent)
{
	//{{AFX_DATA_INIT(CMoniClientDlg)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
	// Note that LoadIcon does not require a subsequent DestroyIcon in Win32
	m_hIcon = AfxGetApp()->LoadIcon(IDI_ICON2);

	started = false;

    m_localInfo.GetInfo();
	m_cTrafficInComing.SetTrafficType(MFNetTraffic::IncomingTraffic);
	m_cTrafficOutGoing.SetTrafficType(MFNetTraffic::OutGoingTraffic);
	m_cTrafficTotal.SetTrafficType(MFNetTraffic::AllTraffic);
	strLocalIpAddr = m_localInfo.m_ipAddress;
}

CMoniClientDlg::~CMoniClientDlg()
{
	GlobalFree(recvbuf);
	closesocket(sock);
	GlobalFree(recvbuf2);
	closesocket(sock2);
}

void CMoniClientDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CMoniClientDlg)
		// NOTE: the ClassWizard will add DDX and DDV calls here
	//}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CMoniClientDlg, CDialog)
	//{{AFX_MSG_MAP(CMoniClientDlg)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_WM_TIMER()
	ON_MESSAGE(WM_GETINFO, OnGetInfo)
	ON_MESSAGE(WM_MY_TRAY_NOTIFICATION,OnTrayNotification)
	ON_COMMAND(ID_POPUP_MENU2, OnPopupMenu2)
	ON_COMMAND(ID_POPUP_MENU3, OnPopupMenu3)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CMoniClientDlg message handlers




BOOL CMoniClientDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// Add "About..." menu item to system menu.

	/*********************************************************************************************
	�޸�ע����������Զ����� BYG 2013-10-22
	*/
	 HKEY   RegKey;   
	 CString   sPath;   
	 GetModuleFileName(NULL,sPath.GetBufferSetLength(MAX_PATH+1),MAX_PATH);   
	 sPath.ReleaseBuffer();   
	 int   nPos;   
	 nPos=sPath.ReverseFind('\\');   
	 sPath=sPath.Left(nPos);   
	 CString   lpszFile=sPath+"\\MoniClient.exe";//���������Ҫ���ҵ�ִ���ļ�����   
	 CFileFind   fFind;   
	 BOOL   bSuccess;   
	 bSuccess=fFind.FindFile(lpszFile);   
	 fFind.Close();   
	 if(bSuccess)   
	 {   
	  CString   fullName;   
	  fullName=lpszFile;   
	  RegKey=NULL;   
	  RegOpenKey(HKEY_LOCAL_MACHINE,"Software\\Microsoft\\Windows\\CurrentVersion\\Run",&RegKey);   
	  RegSetValueEx(RegKey,"MoniClient",0,REG_SZ,(const   unsigned   char*)(LPCTSTR)fullName,fullName.GetLength());//�����������Ҫ��ע�����ע������� 
	  this->UpdateData(FALSE);   
	 }   
	 else   
	 {   
	  //theApp.SetMainSkin();   
	  ::AfxMessageBox("û�ҵ�ִ�г����Զ�����ʧ��");   
	  exit(0);   
	 }  
	 /**********************************************************************************************/


	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		CString strAboutMenu;
		strAboutMenu.LoadString(IDS_ABOUTBOX);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon
	
	// TODO: Add extra initialization here
	//20100407 ding+
	//txtFile = "C:\\anay.txt";
	//if(!w_file.Open(txtFile,CFile::modeCreate | CFile::modeWrite| CFile::typeText))
	//{
	//	//��ʽ�汾��ע�⽫�ⲿ�ִ���ע�͵�
	//	//MessageBox(_T("�ļ�����ʧ��,�޷�д�����ݽ��ռ�¼!"));
	//}
	//else
	//{
	//	CString temp = _T("�󶨵ı���IP��ַΪ:");
	//	temp +=strLocalIpAddr;
	//	w_file.WriteString(temp);
	//	w_file.WriteString(_T("\r\n"));
	//}
	//w_file.Abort();

	//20100427 Ding Yiming+��������Ϣ��¼���ļ���,��ʽ�汾��ע�⽫�ⲿ�ִ���ע�͵�
	/*if(!w_file.Open(_T("C:\\serverinfo.txt"),CFile::modeCreate | CFile::modeWrite| CFile::typeText))
	{
		MessageBox(_T("�ļ�����ʧ��,�޷�д�������Ϣ!"));
	}
	else
	{
		CString temp = _T("��ȡ�ķ�����Ϣ:");
		w_file.WriteString(temp);
		w_file.WriteString(_T("\r\n"));
	}
	w_file.Abort();*/
    
	ReadFile(sPath);
	GetMac(mac);
	StartApplication();
	SetTimer(1, 1000, NULL);
	m_threadHand= (HANDLE)_beginthreadex(NULL, 0, CThreadRecvBroadcastData::ThreadRecvBroadcastData, NULL, 0, NULL);
	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CMoniClientDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialog::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CMoniClientDlg::OnPaint() 
{
	TrayMessage(m_hWnd, NIM_ADD, NULL, "��ش���");
	TrayMessage(m_hWnd, NIM_MODIFY, m_hIcon, "��ش���");
	ShowWindow(SW_HIDE);          //����������ĶԻ���
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, (WPARAM) dc.GetSafeHdc(), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

// The system calls this to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CMoniClientDlg::OnQueryDragIcon()
{
	return (HCURSOR) m_hIcon;
}
char * WSTRToAnsi(wchar_t * Msg)
{
	int len = wcstombs(NULL,Msg,0);
	char * buf = new char[len +1];
	wcstombs(buf,Msg,len);

	buf[len] =0;

	return buf;
}
char * getPName(char* szPname)
{
	
	char *wcschrs = NULL;
	if(strchr(szPname,'\\'))
	{
		wcschrs =strchr(szPname,'\\')+1;
	}
	else
	{
		wcschrs ="";
	}
	return  wcschrs;	
}
CString getPName(CString szPname)
{
	int index = szPname.ReverseFind('\\');
	CString wcschrs;
	if(index >=0)
	{
		return szPname.Mid(index +1);
	}
	return  wcschrs;	
}
void CMoniClientDlg::GetMac(char * mac)
{
	memset(mac,0x00,20);
	PIP_ADAPTER_INFO pAdapterInfo;	
	PIP_ADAPTER_INFO pAdapter = NULL;
	DWORD dwRetVal=0;
	pAdapterInfo = (IP_ADAPTER_INFO *)malloc(sizeof(IP_ADAPTER_INFO));
	ULONG ulOutBufLen = sizeof(IP_ADAPTER_INFO);
	if(GetAdaptersInfo(pAdapterInfo,&ulOutBufLen) != ERROR_SUCCESS)
	{
		DWORD dwerr= GetLastError();
		GlobalFree(pAdapterInfo);
		pAdapterInfo =(IP_ADAPTER_INFO*)malloc(ulOutBufLen);
	}
	if((dwRetVal = GetAdaptersInfo(pAdapterInfo,&ulOutBufLen)) == NO_ERROR)
	{
		pAdapter = pAdapterInfo;
		while(pAdapter)
		{
			//printf("AdapterAddr:\t");
			char temp[20];
			memset(temp,0x00,20);
			for(UINT i=0; i<pAdapter->AddressLength;i++)
			{
				//printf("%02X%c",pAdapter->Address[i],i==pAdapter->AddressLength-1?'\n':'-');
				sprintf(temp,"%02X",pAdapter->Address[i]);
				if(mac ==0)
				{
					strcpy(mac,temp);
				}
				else
				{
					strcat(mac,temp);
				}

			}
			pAdapter = pAdapter->Next;
		}
	}
	else
	{
		printf("call to get Adapters information failed\n");
	}
}

void CMoniClientDlg::ReadFile(CString sPath)
{
	setlocale(LC_ALL,"chs");
	
	CString   configFile=sPath+"\\MonitorAgent.ini";
	std::ifstream inConfigFile(configFile, std::ios::in);   //��Ҫ����#include <fstream>
	//std::ifstream inConfigFile("MonitorAgent.ini", std::ios::in);   //��Ҫ����#include <fstream>
	CString m_sConfigFileName="MonitorAgent.ini";

	CString strProcess;
	CString strService;

	strProcess="";
	strService="";
	int pos,pos2;

	char buf[MAXSIZE];
	if (!inConfigFile)
	{
		AfxMessageBox("ָ���������ļ������ڻ���ִ������飡");
	}

	CString strFile;
	while (inConfigFile && !inConfigFile.eof())
	{
		inConfigFile.getline(buf, MAXSIZE);
		CString str(buf);

		pos=str.Find("ProcessPath=");
		if(pos>=0)
		{
			strProcess = str;

			pos2=strProcess.Find('=');
			strProcess=strProcess.Mid(pos2+1);
		}

		pos=str.Find("ServiceName=");
		if(pos>=0)
		{
			strService = str;

			pos2=strService.Find('=');
			strService=strService.Mid(pos2+1);
		}

		pos=str.Find("ServerIP=");
		if(pos>=0)
		{
			CString sevip = str;

			pos2 = sevip.Find('=');
			sevip = sevip.Mid(pos2+1);

			pos2 = sevip.Find(';');
			m_serverAIP = sevip.Left(pos2);     //��ȡ�׻���IP
			sevip = sevip.Mid(pos2+1);

			pos2 = sevip.Find(';');
			m_serverBIP = sevip.Left(pos2);     //��ȡ�һ���IP
		}

		pos=str.Find("ftpIP=");
		if(pos>=0)
		{
			CString ftpip = str;
			pos2 = ftpip.Find('=');
			m_ftpIP = ftpip.Mid(pos2+1).GetBuffer();
		}

		pos=str.Find("ftpUser=");
		if(pos>=0)
		{
			CString ftpuser = str;
			pos2 = ftpuser.Find('=');
			m_ftpUser = ftpuser.Mid(pos2+1).GetBuffer();
		}

		pos=str.Find("ftpPassword=");
		if(pos>=0)
		{
			CString ftppassword = str;
			pos2 = ftppassword.Find('=');
			m_ftpPassword = ftppassword.Mid(pos2+1).GetBuffer();
		}

		pos=str.Find("ServerPath=");
		if(pos>=0)
		{
			CString serverpath = str;
			pos2 = serverpath.Find('=');
			m_ServerPath = serverpath.Mid(pos2+1).GetBuffer();
		}

		pos=str.Find("LocalPath=");
		if(pos>=0)
		{
			CString localpath = str;
			pos2 = localpath.Find('=');
			m_LocalPath = localpath.Mid(pos2+1).GetBuffer();
		}

		//�������ע�ͺ󲻴������ļ��ж�ȡIP
/*
		pos=str.Find("LocalIpAddr=");
		if(pos>=0)
		{
			strLocalIpAddr = str;

			pos2=strLocalIpAddr.Find('=');
			strLocalIpAddr=strLocalIpAddr.Mid(pos2+1);
		}*/
		setlocale(LC_ALL,"C");
	}

	//��ʼ��m_processInfo����
	while (strProcess.Find(';') != -1)
	{
		PROCESSINFO proInfo;
		int position;
		position = strProcess.Find(';');
		proInfo.processPath = strProcess.Left(position);
		m_processInfo.push_back(proInfo);
		InfoDlg.processInfo.push_back(proInfo);
		strProcess = strProcess.Mid(position + 1);
	}

	//��ʼ��m_serviceInfo����
	while (strService.Find(';') != -1)
	{
		SERVICEINFO serInfo;
		int position;
		position = strService.Find(';');
		serInfo.serviceName = strService.Left(position);
		m_serviceInfo.push_back(serInfo);
		InfoDlg.serviceInfo.push_back(serInfo);
		strService = strService.Mid(position + 1);
	}


	BOOL bSuccess = EnumProcesses(idProcesses, sizeof(idProcesses), &dwNumProcesses);  //EnumProcesses��ȡϵͳ��ÿ�����̶���Ľ��̱�ʶ��
	dwNumProcesses /= sizeof(idProcesses[0]);
	CString sProcessName;
   
	CString str;
	size_t memoryInfo = -1;

	for (int i = 0; i< (int)m_processInfo.size(); i++)
	{
		m_processInfo[i].status = false;
		InfoDlg.processInfo[i].status = false;
		int position;
		position = m_processInfo[i].processPath.ReverseFind('\\');
		m_processInfo[i].processName = m_processInfo[i].processPath.Mid(position + 1);
		InfoDlg.processInfo[i].processName = m_processInfo[i].processPath.Mid(position + 1);
	}

	for (int i = 0; i < (int)dwNumProcesses; i++)
	{
		sProcessName ="";
		CString pName;
	//	 wchar_t  wProcessName[MAX_PATH] = TEXT("");
		HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ,
			FALSE, 
			idProcesses[i]);  
		if (hProcess != NULL)
		{
			HMODULE hModule;
			DWORD cbNeeded;
			//if (EnumProcessModules(hProcess, &hModule, sizeof(hModule), &cbNeeded))
			//{
			//	int nMinBufLength = 1024;
			//	GetModuleBaseName(hProcess, hModule, sProcessName.GetBuffer(nMinBufLength), nMinBufLength);
			//	sProcessName.ReleaseBuffer();
			//}
			if(GetProcessImageFileName(hProcess,  sProcessName.GetBuffer(MAX_PATH) ,MAX_PATH))
			{
					sProcessName.ReleaseBuffer();
				    sProcessName =getPName(sProcessName);
			 }
			for (int j = 0; j < (int)m_processInfo.size(); j++)
			{
				int index = m_processInfo[j].processPath.ReverseFind('\\');
				if (sProcessName.MakeUpper() == m_processInfo[j].processPath.Mid(index + 1).MakeUpper())
				{
					m_processInfo[j].status = true;
					InfoDlg.processInfo[j].status = true;
					GetProcessMemoryInfo(hProcess, &m_processInfo[j].psmemCounters, sizeof(m_processInfo[j].psmemCounters));
					FILETIME creationTime;
					FILETIME exitTime;
					if (GetProcessTimes(hProcess, &creationTime, &exitTime, &kernelTime, &userTime))
					{
						m_processInfo[j].uKernelTime.LowPart = kernelTime.dwLowDateTime;
						m_processInfo[j].uKernelTime.HighPart = kernelTime.dwHighDateTime;
						m_processInfo[j].uUserTime.LowPart = userTime.dwLowDateTime;
						m_processInfo[j].uUserTime.HighPart = userTime.dwHighDateTime;

						InfoDlg.processInfo[j].uKernelTime.LowPart = kernelTime.dwLowDateTime;
						InfoDlg.processInfo[j].uKernelTime.HighPart = kernelTime.dwHighDateTime;
						InfoDlg.processInfo[j].uUserTime.LowPart = userTime.dwLowDateTime;
						InfoDlg.processInfo[j].uUserTime.HighPart = userTime.dwHighDateTime;
					}
				}
			}
			CloseHandle(hProcess);
		}
	}
	inConfigFile.close();
}

void CMoniClientDlg::StartApplication()
{
	//20100407 ding+
	/*AfxBeginThread(RecvInfoFromSvr, (void*)(this));
	AfxBeginThread(RecvSvrStruct,(void*)(this));*/

	AfxBeginThread(RecvInfoFromSvr, 0);
	AfxBeginThread(RecvSvrStruct,0);

	iPort = DEFAULT_PORT;
	dwLength = DEFAULT_BUFFER_LENGTH;

	recipient.sin_family = AF_INET;
	recipient.sin_port = htons((short)iPort);
	recipient.sin_addr.s_addr = inet_addr((LPCSTR)"239.255.0.11");

	SOCKADDR_IN  local;
	local.sin_family = AF_INET;
	local.sin_port = htons(DEFAULT_PORT2);
	local.sin_addr.s_addr= inet_addr(strLocalIpAddr);

	if (WSAStartup(MAKEWORD(2, 2), &wsd) != 0)      //��������ԭ���ĳ�������ע���˵�
	{
		return;
	}

	if( (sk = socket( AF_INET, SOCK_DGRAM, 0) ) < 0 )
	{
		return ;
	}

	started = true;

	//if( bind(sk, (struct sockaddr *)&local, sizeof(local)) < 0 )
	if( bind(sk, (struct sockaddr *)&local, sizeof(local)) == SOCKET_ERROR )
	{
		return ;
	}

	if (setsockopt(sk,IPPROTO_IP,IP_MULTICAST_IF,(char*)&local.sin_addr.s_addr,sizeof(local.sin_addr.s_addr)) == SOCKET_ERROR)
	{
		return ;
	}

	int ttl = 16;
	if (setsockopt(sk,IPPROTO_IP,IP_MULTICAST_TTL,(char*)&ttl,sizeof(ttl)) == SOCKET_ERROR)
	{
		return ;
	}

}

//���������������ݵ��鲥��ַΪ"239.255.0.11",�˿ں�Ϊ5150
void CMoniClientDlg::OnTimer(UINT nIDEvent) 
{
	// TODO: Add your message handler code here and/or call default
	bsent = false;
	if (!started)
	{
		return;
	}
	else
	{
		sendbuf = (char*)GlobalAlloc(GMEM_FIXED, dwLength);
		memset(sendbuf, '*', dwLength);
		if (m_localInfo.m_ipAddress == m_serverAIP || m_localInfo.m_ipAddress == m_serverBIP)  //���ڻ���Ϊ˫���ȱ�������֮һ
		{
			//�����һ���IP����˫��״̬����Ϣ����
			GetServerStatus();
			bsent = true;		
		}
		
		InfoDlg.ipaddress = m_localInfo.m_ipAddress;
		InfoDlg.hostname = m_localInfo.m_hostName;

		//CpuInfo();
		GetCpuUsage();
		InfoDlg.cpuRate = m_cpuRate;
		DiskInfo();
		InfoDlg.diskInfo = m_diskInfo;
		MemoryInfo();
		InfoDlg.memoryInfo = m_memoryInfo;
		ProcessInfo();
		ServiceInfo();

		if (bsent)      //Ҫ�����ȱ�������˫��״̬����
		{
			CString transtr = "";
			//��ʽΪ �׻�IP#�һ�IP#�׻�״̬#�һ�״̬#ר������1��״̬#ר������2��״̬#�׻��ؼ�����״̬
            //       #�׻���������״̬#˫��������ʽ#�һ��ؼ�����״̬#�һ���������״̬
			strcpy(sendbuf, (LPCSTR)(m_serverAIP + "#"));
			strcat(sendbuf, (LPCSTR)(m_serverBIP + "#"));
			transtr.Format("%d",StatusOfA);
			strcat(sendbuf, (LPCSTR)(transtr + "#"));
			transtr.Format("%d",StatusOfB);
			strcat(sendbuf, (LPCSTR)(transtr + "#"));
			transtr.Format("%d",Line1Status);
			strcat(sendbuf, (LPCSTR)(transtr + "#"));
			transtr.Format("%d",Line2Status);
			strcat(sendbuf, (LPCSTR)(transtr + "#"));
			transtr.Format("%d",SoftStatusOfA);
			strcat(sendbuf, (LPCSTR)(transtr + "#"));
			transtr.Format("%d",NetStatusOfA);
			strcat(sendbuf, (LPCSTR)(transtr + "#"));
			transtr.Format("%d",Mode);
			strcat(sendbuf, (LPCSTR)(transtr + "#"));
			transtr.Format("%d",SoftStatusOfB);
			strcat(sendbuf, (LPCSTR)(transtr + "#"));
			transtr.Format("%d",NetStatusOfB);
			strcat(sendbuf, (LPCSTR)transtr);

			strcat(sendbuf, (LPCSTR)("$" + m_localInfo.m_hostName + "$"));
		}
		else
		{
			strcpy(sendbuf, (LPCSTR)("$" + m_localInfo.m_hostName + "$")); //��ע�͵�
			
		}
		//strcpy(sendbuf, (LPCSTR)(m_localInfo.m_hostName + "$")); //��ʱ����

		strcat(sendbuf, (LPCSTR)(m_cpuRate + "$"));
		strcat(sendbuf, (LPCSTR)(m_diskInfo + "$"));
		strcat(sendbuf, (LPCSTR)(m_memoryInfo + "$"));

		CString outstr;     //test
		outstr =strLocalIpAddr + m_localInfo.m_hostName + m_cpuRate + " " + m_diskInfo + " " + m_memoryInfo + "\r\n";  //test
 
		//������Ϣ
		for (int i = 0; i < (int)m_processInfo.size(); i++)
		{
			if (m_processInfo[i].status == true)
			{
				strcat(sendbuf, (LPCSTR)(m_processInfo[i].processName + "#"));
				outstr += m_processInfo[i].processName + "#";  //test
				CString str;
				str.Format("%d", m_processInfo[i].psmemCounters.WorkingSetSize / 1024);
				strcat(sendbuf, (LPCSTR)(str + "#"));
				outstr += str + "#"; //test
				str.Format("%d", (int)(m_processInfo[i].processCPURate * 100.0));
				strcat(sendbuf, (LPCSTR)str);
				outstr += str;    //test
			}
			else
			{
				strcat(sendbuf, (LPCSTR)(m_processInfo[i].processName + "#" + "NOT EXECUTED"));
				outstr += m_processInfo[i].processName + "#" + "NOT EXECUTED";  //test
			}
			strcat(sendbuf, "&");
			outstr += "&\r\n";  //test
		}
		strcat(sendbuf, "$");
		outstr += "$";  //test

		//������Ϣ
		for (int i = 0; i < (int)m_serviceInfo.size(); i++)
		{
			strcat(sendbuf, (LPCSTR)(m_serviceInfo[i].serviceName + "#"));
			outstr += m_serviceInfo[i].serviceName + "#";  //test
			CString str;
			str.Format("%d", m_serviceInfo[i].status);
			strcat(sendbuf, (LPCSTR)(str + "&"));
			outstr += str + "&\r\n";       //test
		}
		strcat(sendbuf, "$");
		outstr += "$";  //test

		//������Ϣ
		CString str;
		strcat(sendbuf, (LPCSTR)(m_cTrafficClass.Interfaces.GetAt(m_cTrafficClass.Interfaces.FindIndex(0)) + "#"));
		outstr += m_cTrafficClass.Interfaces.GetAt(m_cTrafficClass.Interfaces.FindIndex(0)) + "#";  //test
		str.Format("%d", m_cTrafficClass.Bandwidths.GetAt(m_cTrafficClass.Bandwidths.FindIndex(0)));
		strcat(sendbuf, (LPCSTR)(str + "#"));
		outstr += str + "#";  //test

		InfoDlg.ifs = m_cTrafficClass.Interfaces.GetAt(m_cTrafficClass.Interfaces.FindIndex(0));
		InfoDlg.bandwides = str;

		str.Format("%.1f", (double)(m_cTrafficInComing.GetTraffic(0) / 1024.));
		strcat(sendbuf, (LPCSTR)(str + "#"));
		outstr += str + "#";  //test

		InfoDlg.intraffic = str + "KB/sec";

		str.Format("%.1f", (double)(m_cTrafficOutGoing.GetTraffic(0) / 1024.));
		strcat(sendbuf, (LPCSTR)(str + "#"));
		outstr += str + "#";  //test

		InfoDlg.outtraffic = str + "KB/sec";

		TRACE("\n%.1f\n", (double)(m_cTrafficTotal.GetTraffic(0) / 1024.));
		str.Format("%.1f", (double)(m_cTrafficClass.GetTraffic(0) / 1024.));
		strcat(sendbuf, (LPCSTR)(str + "#"));
		outstr += str + "#";  //test
		
		strcat(sendbuf, (LPCSTR)( "$")); // 
		strcat(sendbuf, (LPCSTR)( mac)); // 
		
		

		InfoDlg.alltraffic = str + "KB/sec";

		int ret;
		ret = sendto(sk, sendbuf, dwLength, 0, (SOCKADDR*)&recipient, sizeof(recipient));
		
		GetDlgItem(IDC_EDIT1)->SetWindowText(outstr);  //test
		GlobalFree(sendbuf);
	}
	CDialog::OnTimer(nIDEvent);
}

__int64 CompareFileTime(FILETIME time1, FILETIME time2)
{
	__int64 a = time1.dwHighDateTime<<32|time1.dwLowDateTime;
	__int64 b = time2.dwHighDateTime<<32|time2.dwLowDateTime;

	return b-a;

}

void CMoniClientDlg::CpuInfo()
{
	LONGLONG ( __stdcall *NtQuerySystemInformation )( DWORD, PVOID, DWORD, DWORD );


	NtQuerySystemInformation = (LONGLONG (__stdcall*)(DWORD,PVOID,DWORD,DWORD))GetProcAddress(
		GetModuleHandle("ntdll"),
		"NtQuerySystemInformation"
		);
	if (!NtQuerySystemInformation)
		return;

	// get number of processors in the system
	status = NtQuerySystemInformation(SystemBasicInformation,&SysBaseInfo,sizeof(SysBaseInfo),NULL);
	if (status != NO_ERROR)
		return;

	// get new system time
	status = NtQuerySystemInformation(SystemTimeInformation,&SysTimeInfo,sizeof(SysTimeInfo),0);
	if (status!=NO_ERROR)
		return;

	// get new CPU's idle time
	status = NtQuerySystemInformation(SystemPerformanceInformation,&SysPerfInfo,sizeof(SysPerfInfo),NULL);
	if (status != NO_ERROR)
		return;

	// if it's a first call - skip it
	if (liOldIdleTime.QuadPart != 0)
	{
		// CurrentValue = NewValue - OldValue
		dbIdleTime = Li2Double(SysPerfInfo.liIdleTime) - Li2Double(liOldIdleTime);
		dbSystemTime = Li2Double(SysTimeInfo.liKeSystemTime) - Li2Double(liOldSystemTime);

		// CurrentCpuIdle = IdleTime / SystemTime
		dbIdleTime = dbIdleTime / dbSystemTime;

		// CurrentCpuUsage% = 100 - (CurrentCpuIdle * 100) / NumberOfProcessors
		dbIdleTime = 100.0 - dbIdleTime * 100.0 / (double)SysBaseInfo.bKeNumberProcessors + 0.5;
		if(dbIdleTime <0)
		{
		//	m_cpuRate.Format("%3d", mm);
			liOldIdleTime = SysPerfInfo.liIdleTime;
			liOldSystemTime = SysTimeInfo.liKeSystemTime;	
			return;
		}
		m_cpuRate.Format("%d", (UINT)dbIdleTime);
	}

	// store new CPU's idle and system time
	liOldIdleTime = SysPerfInfo.liIdleTime;
	liOldSystemTime = SysTimeInfo.liKeSystemTime;

}

void CMoniClientDlg::GetCpuUsage()
{
	FILETIME idleTime;
	FILETIME kernelTime;
	FILETIME userTime;
	GetSystemTimes(&idleTime, &kernelTime, &userTime);
	
	//m_preIdleTime = idleTime;
	//m_preKernelTime = kernelTime;
	//m_preUserTime = userTime;
	//Sleep(1000);
	//GetSystemTimes(&idleTime, &kernelTime, &userTime); 

	int idle = CompareFileTime(m_preIdleTime,idleTime);
	int kernel = CompareFileTime(m_preKernelTime,kernelTime);
	int user = CompareFileTime(m_preUserTime,userTime);

	if(kernel + user == 0)
	{ 
		m_cpuRate.Format("%d", 0);
	}
	else
	{
		double cpu = (kernel + user - idle) * 100.0 / (kernel * 1.0 + user * 1.0 );
		m_cpuRate.Format("%d", (UINT)cpu);
	}

	m_preIdleTime = idleTime;
	m_preKernelTime = kernelTime;
	m_preUserTime = userTime;
}

void CMoniClientDlg::DiskInfo()
{
	char szDrives[128];
	char* pDrive;
	ULARGE_INTEGER nTotalBytes, nTotalFreeBytes, nTotalAvailable;
	nAllFreeBytes = 0;		//���д��̵Ŀ������ɿռ���ܺ�
	nAllBytes = 0;			//����Ӳ�̵��ܵĿռ��ܺ� 
	CString StrDriveName = "";
	if ( !GetLogicalDriveStrings( sizeof (szDrives), szDrives) )
	{
		return;
	}

	pDrive = szDrives;
	while ( *pDrive )
	{
		CString Str;
		Str.Format("%s", pDrive);
		if ( ::GetDriveType( (LPCSTR)Str ) == DRIVE_FIXED )
		{
			if ( ::GetDiskFreeSpaceEx( (LPCSTR)Str, &nTotalAvailable, &nTotalBytes, &nTotalFreeBytes) )
			{
				nAllFreeBytes += nTotalFreeBytes.QuadPart;
				nAllBytes += nTotalBytes.QuadPart;
			}
		}
		StrDriveName += Str + ", ";
		pDrive += strlen ( pDrive ) + 1;
	}
	CString strAllFreeBytes, strAllBytes;
	strAllFreeBytes.Format("%d", nAllFreeBytes / 1024 / 1024 );
	strAllBytes.Format("%d", nAllBytes / 1024 / 1024);
	m_diskInfo = strAllFreeBytes + '/' + strAllBytes;
}

void CMoniClientDlg::MemoryInfo()
{
	CString str,temp; 

	/*MEMORYSTATUS memStatus;  
	memStatus.dwLength=sizeof(MEMORYSTATUS);
	::GlobalMemoryStatus(&memStatus);
	str.Format("%d/%d", memStatus.dwAvailPhys / 1024 / 1024, memStatus.dwTotalPhys / 1024 / 1024);*/

	//20101115 Ding Yiming +"Ex"
	MEMORYSTATUSEX memStatus;
	memStatus.dwLength = sizeof (memStatus);
	::GlobalMemoryStatusEx(&memStatus);
	str.Format("%d", memStatus.ullAvailPhys / 1024 / 1024);
	temp.Format("%d",memStatus.ullTotalPhys / 1024 / 1024);
	
	m_memoryInfo = str + _T("/") + temp;
}

void CMoniClientDlg::ProcessInfo()
{
	//EnumProcesses��ȡϵͳ��ÿ�����̶���Ľ��̱�ʶ��
	BOOL bSuccess = ::EnumProcesses(idProcesses, sizeof(idProcesses), &dwNumProcesses);
	dwNumProcesses /= sizeof(idProcesses[0]);
	CString sProcessName;
	int pInfoIndex=0;
	vector<HANDLE>cpupid;

	for (int i = 0; i < (int)m_processInfo.size(); i++)
	{
		m_processInfo[i].status = false;
		InfoDlg.processInfo[i].status = false;
	}
    for (pInfoIndex= 0; pInfoIndex < (int)m_processInfo.size(); pInfoIndex++)
	{
		cpupid.clear();
		int index = m_processInfo[pInfoIndex].processPath.ReverseFind('\\');
		for (int i = 0; i < (int)dwNumProcesses; i++)
		{
			//OpenProcess��һ���Ѿ����ڵĽ��̶���,����ָ�����̵Ĵ򿪵ľ��,�򿪲��ɹ����ؿ�
			//PROCESS_QUERY_INFORMATION ������GetExitCodeProcess��GetPriorityClass������ʹ�ý��̾���ӽ��̶����ж�ȡ��Ϣ
			//PROCESS_VM_READ ������ReadProcessMemory������ʹ�ý��̾����ȡ���̵������ڴ�				
			sProcessName ="";
			HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ,
				FALSE, 
				idProcesses[i]);
			if (hProcess != NULL)
			{
				//HMODULE hModule;
				//DWORD cbNeeded;
				//EnumProcessModules�����ָ��������ÿ��ģ��ľ��
				//if (::EnumProcessModules(hProcess, &hModule, sizeof(hModule), &cbNeeded))
				//{
				//	int nMinBufLength = 1024;
				//	//GetModuleBaseName ���ָ��ģ��Ļ�������
				//	::GetModuleBaseName(hProcess, hModule, sProcessName.GetBuffer(nMinBufLength), nMinBufLength);
				//	sProcessName.ReleaseBuffer();
				//}
				int pid =idProcesses[i];
				if(GetProcessImageFileName(hProcess,  sProcessName.GetBuffer(MAX_PATH) ,MAX_PATH))
				{
						sProcessName.ReleaseBuffer();
						sProcessName =getPName(sProcessName);
				 }				
				
				if (sProcessName.MakeUpper() == m_processInfo[pInfoIndex].processPath.Mid(index + 1).MakeUpper())   //��ת���ɴ�д����бȽ�
				{
						cpupid.push_back(hProcess);
						m_processInfo[pInfoIndex].status = true;
						InfoDlg.processInfo[pInfoIndex].status = true;
						m_processInfo[pInfoIndex].dwProcessID = pid;
						InfoDlg.processInfo[pInfoIndex].dwProcessID = pid;
						::GetProcessMemoryInfo(hProcess, &m_processInfo[pInfoIndex].psmemCounters, sizeof(m_processInfo[pInfoIndex].psmemCounters));
					}
				}
				//CloseHandle(hProcess);
			}
		for(unsigned int vindex=0;vindex<cpupid.size();vindex++)
		{
			
			InfoDlg.processInfo[pInfoIndex].psmemCounters = m_processInfo[pInfoIndex].psmemCounters;	
			FILETIME creationTime;
			FILETIME exitTime;
			ULARGE_INTEGER uKernelTime, uUserTime;
			if (GetProcessTimes(cpupid[vindex], &creationTime, &exitTime, &kernelTime, &userTime))
			{
				memcpy(&uKernelTime, &kernelTime, sizeof(kernelTime));
				memcpy(&uUserTime, &userTime, sizeof(userTime));

				m_processInfo[pInfoIndex].cpuTime.QuadPart = uKernelTime.QuadPart + uUserTime.QuadPart
					- m_processInfo[pInfoIndex].uKernelTime.QuadPart - m_processInfo[pInfoIndex].uUserTime.QuadPart;
				InfoDlg.processInfo[pInfoIndex].cpuTime.QuadPart = uKernelTime.QuadPart + uUserTime.QuadPart
					- m_processInfo[pInfoIndex].uKernelTime.QuadPart - m_processInfo[pInfoIndex].uUserTime.QuadPart;

				m_processInfo[pInfoIndex].uKernelTime = uKernelTime;
				InfoDlg.processInfo[pInfoIndex].uKernelTime = uKernelTime;

				m_processInfo[pInfoIndex].uUserTime = uUserTime;
				InfoDlg.processInfo[pInfoIndex].uUserTime = uUserTime;

				float cpuvalue= (float)m_processInfo[pInfoIndex].cpuTime.QuadPart / 10000000 / (double)SysBaseInfo.bKeNumberProcessors + 0.005;
				if(cpuvalue >=0)
				{
					m_processInfo[pInfoIndex].processCPURate = cpuvalue>1?0.0:cpuvalue;
					InfoDlg.processInfo[pInfoIndex].processCPURate = cpuvalue?0.0:cpuvalue;
					break;
				}
				else
				{
				}
			}


					//FILETIME CreateTime, ExitTime, KernelTime, UserTime;
					//LARGE_INTEGER lgKernelTime;
					//LARGE_INTEGER lgUserTime;
					//LARGE_INTEGER lgCurTime;
					//BOOL bRetCode = FALSE;
					//if (GetProcessTimes(cpupid[vindex], &CreateTime, &ExitTime, &KernelTime, &UserTime))
					//{
					//	lgKernelTime.HighPart = KernelTime.dwHighDateTime;
					//	lgKernelTime.LowPart = KernelTime.dwLowDateTime;

					//	lgUserTime.HighPart = UserTime.dwHighDateTime;
					//	lgUserTime.LowPart = UserTime.dwLowDateTime;

					//	lgCurTime.QuadPart = (lgKernelTime.QuadPart + lgUserTime.QuadPart) /10000;
					//	
					//	DWORD dwCurrentTickCount = GetTickCount();
					//	DWORD dwElapsedTime = dwCurrentTickCount - m_processInfo[pInfoIndex].sdwTickCountOld;

					//	int result = (int)((lgCurTime.QuadPart - m_processInfo[pInfoIndex].cpuTime.QuadPart) * 100/dwElapsedTime);
					//	m_processInfo[pInfoIndex].cpuTime = lgCurTime;
					//	m_processInfo[pInfoIndex].sdwTickCountOld = dwCurrentTickCount;
					//	double proNum = (double) SysBaseInfo.bKeNumberProcessors;
					//	double cpuvalue= result / proNum;


					//}

		}
		}	
}

static uint64_t file_time_2_utc(const FILETIME* ftime)
{
	LARGE_INTEGER li;

	li.LowPart = ftime->dwLowDateTime;
	li.HighPart = ftime->dwHighDateTime;

	return li.QuadPart;
}

static int get_processor_number()
{
	SYSTEM_INFO info;
	GetSystemInfo(&info);
	return (int)info.dwNumberOfProcessors;
}


int CMoniClientDlg::Get_cpu_usage(int pid)
{
	static int processor_count_ = -1;

	static int64_t last_time_ =0;
	static int64_t last_system_time_ =0;

	FILETIME now;
	FILETIME creation_time;
	FILETIME exit_time;
	FILETIME kernel_time;
	FILETIME user_time;
	int64_t time;
	int64_t system_time;
	int64_t system_time_delta;
	int64_t time_delta;


	int cpu =-1;

	if(processor_count_ ==-1)
	{
		processor_count_ = get_processor_number();
	}
	GetSystemTimeAsFileTime(&now);
	
	HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS,false,pid);
	if(!GetProcessTimes(hProcess, &creation_time,&exit_time,&kernel_time,&user_time))
	{
		return -1;
	}
	system_time = (file_time_2_utc(&kernel_time) + file_time_2_utc(&user_time) )/ processor_count_;
	time = file_time_2_utc(&now);


	if ( (last_system_time_ ==0 )|| (last_time_ ==0))
	{
		last_system_time_ = system_time;
		last_time_ = time;
		return -1;
	}

	system_time_delta = system_time - last_system_time_;
	time_delta = time - last_time_;

	if(time_delta ==0)
	{
		return -1;
	}

	cpu = (int)((system_time_delta * 100 + time_delta/2)/time_delta);
	last_system_time_ = system_time;
	last_time_ = time;
	return cpu;
	
}
void CMoniClientDlg::ServiceInfo()
{
	//��m_serviceInfo��ÿ�������״̬��Ϊ0
	for (int j = 0; j < (int)m_serviceInfo.size(); j++)
	{
		m_serviceInfo[j].status = 0;
		InfoDlg.serviceInfo[j].status = 0;
	}

	DWORD cbBytesNeeded;
	DWORD resumeHandle = 0;

	//OpenSCManager����������ָ��������ϵķ�����ƹ�������һ������,����ָ���ķ�����ƹ��������ݿ�
	//��һ������ΪNULL��ʾ���ӱ��ؼ�����ķ�����ƹ�����
	//�ڶ�������ΪNULL��ʾĬ�ϴ�SERVICES_ACTIVE_DATABASE���ݿ�
	//����������SC_MANAGER_ENUMERATE_SERVICE ��ʾ���Ե��ú���EnumServicesStatus�г����ݿ��еķ���
	SC_HANDLE scHandle = OpenSCManager(NULL, NULL, SC_MANAGER_ENUMERATE_SERVICE);

	if(scHandle!=NULL)
	{
		//EnumServicesStatus�����о�ָ���ķ�����ƹ��������ݿ��еķ���.�ṩÿ����������ֺ�״̬
		if (!::EnumServicesStatus(scHandle, SERVICE_WIN32, SERVICE_STATE_ALL, enumServiceStatus, 
			sizeof(enumServiceStatus), &cbBytesNeeded, &servicesReturned, &resumeHandle))
		{
			DWORD err = GetLastError();
			return;
		}

		for (int i = 0; i < (int)servicesReturned; i++)
		{
			for (int j = 0; j < (int)m_serviceInfo.size(); j++)
			{
				if (enumServiceStatus[i].lpDisplayName == m_serviceInfo[j].serviceName)
				{	
					//20100427 Ding Yiming+��������Ϣ��¼���ļ���,��ʽ�汾��ע�⽫�ⲿ�ִ���ע�͵�
					/*if(w_file.Open(_T("C:\\serverinfo.txt"),CFile::modeReadWrite| CFile::typeText))
					{
						CString temp = _T("");
						temp.Format(_T("������ʾ��:%s---������:%s"),enumServiceStatus[i].lpDisplayName,enumServiceStatus[i].lpServiceName);
						w_file.Seek(0,CFile::end);
						w_file.WriteString(temp);
						w_file.WriteString(_T("\r\n"));
					}
					w_file.Abort();*/

					m_serviceInfo[j].srvName = enumServiceStatus[i].lpServiceName;

					if (enumServiceStatus[i].ServiceStatus.dwCurrentState == SERVICE_CONTINUE_PENDING)
					{
						m_serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
						InfoDlg.serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
					}
					if (enumServiceStatus[i].ServiceStatus.dwCurrentState == SERVICE_PAUSE_PENDING)
					{
						m_serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
						InfoDlg.serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
					}
					if (enumServiceStatus[i].ServiceStatus.dwCurrentState == SERVICE_PAUSED)
					{
						m_serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
						InfoDlg.serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
					}
					if (enumServiceStatus[i].ServiceStatus.dwCurrentState == SERVICE_RUNNING)        //4
					{
						m_serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
						InfoDlg.serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
					}
					if (enumServiceStatus[i].ServiceStatus.dwCurrentState == SERVICE_START_PENDING)
					{
						m_serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
						InfoDlg.serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
					}
					if (enumServiceStatus[i].ServiceStatus.dwCurrentState == SERVICE_STOP_PENDING)
					{
						m_serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
						InfoDlg.serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
					}
					if (enumServiceStatus[i].ServiceStatus.dwCurrentState == SERVICE_STOPPED)       //1
					{
						m_serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
						InfoDlg.serviceInfo[j].status = enumServiceStatus[i].ServiceStatus.dwCurrentState;
					}
				}
			}
		}
		CloseServiceHandle(scHandle);
	}
}

void CMoniClientDlg::StartProcess(LPCSTR processName)
{
	CFileFind find;
	BOOL bFind;
	CString strPath;
	CString strProcessName(processName);
	int index = -1;
	for (int i = 0; i < (int)m_processInfo.size(); i++)
	{
		if (m_processInfo[i].status == false && m_processInfo[i].processName == strProcessName)
		{
			strPath = m_processInfo[i].processPath;
			index = i;
		}
	}
	if (index == -1)
	{
		return;
	}
	int pos = -1;
	pos = strPath.ReverseFind('\\');

	CString strDirectory;
	strDirectory = strPath.Left(pos) + '\\';
	bFind = find.FindFile(strDirectory);

	ShellExecuteA(NULL, "open", (LPCSTR)strProcessName, "", (LPCSTR)strDirectory, SW_SHOWNORMAL);
	m_processInfo[index].status = true;
}

//ע��:����ֹͣ����ʱ ������������Ͳ�����"�ѽ���" ��Ҫ���Ƿ�������(��ش�������Ҫ���Ƿ�����ʾ����) ����ߺܶ�������ǲ�һ�µ�
void CMoniClientDlg::StartServices(LPCSTR serviceName)
{
	CString strServiceDisplay(serviceName);    //������ʾ����
	CString strService;                        //��������

	for (int i = 0; i < (int)m_serviceInfo.size(); i++)
	{
		if (m_serviceInfo[i].serviceName == strServiceDisplay)
		{
			strService = m_serviceInfo[i].srvName;
		}
	}

	SC_HANDLE scHandle = OpenSCManager(NULL, SERVICES_ACTIVE_DATABASE, SC_MANAGER_ALL_ACCESS);
	if(scHandle == NULL)
	{
		return;
	}

/*	char* keyName = NULL;
	DWORD keyNameLength = 0;
	//GetServiceKeyName �����ӷ������ʾ���ƻ�ȡ�����������������ע����д洢��
	if(::GetServiceKeyName(scHandle, serviceName, keyName, &keyNameLength) == false)
	{
		//�������д���Ϊ������
		DWORD m = GetLastError();
		CString str;
		str.Format("%d",m);
		AfxMessageBox(str);
		return;
	}*/

	//OpenService ��һ���Ѿ����ڵķ��� SERVICE_START���Ե���StartService, SERVICE_QUERY_STATUS���Ե���QueryServiceStatus	
	//SC_HANDLE serviceHandle = OpenService(scHandle, "Messenger", SERVICE_START | SERVICE_QUERY_STATUS);
	SC_HANDLE serviceHandle = OpenService(scHandle, strService, SERVICE_ALL_ACCESS); //SERVICE_START | SERVICE_QUERY_STATUS);
	if(serviceHandle == NULL)
	{ 
		return;
	}
	SERVICE_STATUS serviceStatus;
	if(::QueryServiceStatus(serviceHandle, &serviceStatus) == false)	  //QueryServiceStatus ��ȡָ������ĵ�ǰ״̬
	{
		::CloseServiceHandle( scHandle);
		::CloseServiceHandle( serviceHandle);
		return;

	}

	if( serviceStatus.dwCurrentState == SERVICE_STOPPED)
	{
		// ��������
		if( !StartService( serviceHandle, 0, NULL))
		{
/*			DWORD m = GetLastError();
			CString str;
			str.Format("%d",m);
			AfxMessageBox(str);*/
			//AfxMessageBox( "start service error��");
			CloseServiceHandle( scHandle);
			CloseServiceHandle( serviceHandle);
			return;
		}
		// �ȴ���������
/*		while( ::QueryServiceStatus( serviceHandle, &serviceStatus) == TRUE)
		{
			Sleep( serviceStatus.dwWaitHint);
			if( serviceStatus.dwCurrentState == SERVICE_RUNNING)
			{
				CloseServiceHandle( scHandle);
				CloseServiceHandle( serviceHandle);
			}
		}*/
	}	

	for (int i = 0; i < (int)m_serviceInfo.size(); i++)
	{
		if (m_serviceInfo.at(i).serviceName == strServiceDisplay)
		{
			m_serviceInfo.at(i).status = 4;
		}
	}
}

void CMoniClientDlg::CloseProcess(LPCSTR processName)
{
	CString strProcessName(processName);
	DWORD dwProcessID;
	for (int i = 0; i < (int)m_processInfo.size(); i++)
	{
		if (m_processInfo[i].status == true && m_processInfo[i].processName == strProcessName)
		{

			m_processInfo[i].status = false;
			dwProcessID = m_processInfo[i].dwProcessID;
		}
	}
	HANDLE hProcess = OpenProcess(PROCESS_TERMINATE, FALSE, dwProcessID);
	TerminateProcess(hProcess, 0);
	CloseHandle(hProcess);
}

void CMoniClientDlg::CloseService(LPCSTR serviceName)
{
	CString strServiceDisplay(serviceName);    //������ʾ����
	CString strService;                        //��������

	for (int i = 0; i < (int)m_serviceInfo.size(); i++)
	{
		if (m_serviceInfo[i].serviceName == strServiceDisplay)
		{
			strService = m_serviceInfo[i].srvName;
		}
	}

	SC_HANDLE scHandle = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);
	if(scHandle == NULL)
	{
		return;
	}

/*	char* keyName = NULL;
	DWORD keyNameLength = 0;
	if(::GetServiceKeyName(scHandle, serviceName, keyName, &keyNameLength) == false)
	{
		//�������д���Ϊ������
		DWORD m = GetLastError();
		CString str;
		str.Format("%d",m);
		AfxMessageBox(str);
		return;
	}
	TRACE("%s\n", keyName);*/

	SC_HANDLE serviceHandle = OpenService(scHandle, strService, SERVICE_ALL_ACCESS); //SERVICE_STOP);
	if(serviceHandle == NULL)
	{ 
/*		DWORD m = GetLastError();
		CString str;
		str.Format("%d",m);
		AfxMessageBox(str);*/
		return;
	}
	SERVICE_STATUS serviceStatus;
	if (!ControlService(serviceHandle, SERVICE_CONTROL_STOP, &serviceStatus))
	{
/*		DWORD m = GetLastError();
		CString str;
		str.Format("%d",m);
		AfxMessageBox(str);*/
		return;
	}
	CloseServiceHandle(scHandle);
	CloseServiceHandle(serviceHandle);
	for (int i = 0; i < (int)m_serviceInfo.size(); i++)
	{
		if (m_serviceInfo.at(i).serviceName == strServiceDisplay)
		{
			m_serviceInfo.at(i).status = 1;       //ԭ��Ϊ7
		}
	}
}

LRESULT CMoniClientDlg::OnGetInfo(WPARAM wParam, LPARAM lParam)
{
	char* name = (char*)lParam;
	CString str((char*)lParam);

	//20100407 ding+
	/*CString temp,string;
	CTime tm;
    tm = CTime::GetCurrentTime();
	temp = tm.Format("%Y��%m��%d��%H:%M:%S");*/

	if (name[0] == 'P')				//�����һ���ַ���P����ô������ֹͣ�������
	{
		if (name[1] == 'S')
		{
			//20100407 ding+
			/*if(w_file.Open(txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				string = &name[3];
				temp=temp+_T("--��������--")+string;
				w_file.Seek(0,CFile::end);
				w_file.WriteString(temp);
				w_file.WriteString(_T("\r\n"));
			}
			w_file.Abort();*/

			StartProcess(&name[3]);
		}
		else if (name[1] == 'C')
		{
			//20100407 ding+
			/*if(w_file.Open(txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				string = &name[3];
				temp=temp+_T("--�رս���--")+string;
				w_file.Seek(0,CFile::end);
				w_file.WriteString(temp);
				w_file.WriteString(_T("\r\n"));
			}
			w_file.Abort();*/

			CloseProcess(&name[3]);
		}
	}
	else if (name[0] == 'S')		//�����һ���ַ���S����ô������ֹͣ�������
	{
		if (name[1] == 'S')
		{
			//20100407 ding+
			/*if(w_file.Open(txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				string = &name[3];
				temp=temp+_T("--��������--")+string;
				w_file.Seek(0,CFile::end);
				w_file.WriteString(temp);
				w_file.WriteString(_T("\r\n"));
			}
			w_file.Abort();*/

			StartServices(&name[3]);
		}
		else if (name[1] == 'C')
		{
			//20100407 ding+
			/*if(w_file.Open(txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				string = &name[3];
				temp=temp+_T("--�رշ���--")+string;
				w_file.Seek(0,CFile::end);
				w_file.WriteString(temp);
				w_file.WriteString(_T("\r\n"));
			}
			w_file.Abort();*/

			CloseService(&name[3]);			
		}
	}
	else if(name[0] == 'B')       //���ǰ�����ַ���B��S,��ô�����������л�
	{
		if(name[1] == 'S')
		{
			//20100407 ding+
			/*if(w_file.Open(txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				temp=temp+_T("--�����������л�");
				w_file.Seek(0,CFile::end);
				w_file.WriteString(temp);
				w_file.WriteString(_T("\r\n"));
			}
			w_file.Abort();*/

			BegSwitch();
		}
	}
	else if(name[0] == 'F')
	{
		if(name[1] == 'C')
		{
			//20100407 ding+
			/*if(w_file.Open(txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				temp=temp+_T("--ǿ�Ʊ���Ϊ����");
				w_file.Seek(0,CFile::end);
				w_file.WriteString(temp);
				w_file.WriteString(_T("\r\n"));
			}
			w_file.Abort();*/

			ForceZhu();          //���ǰ�����ַ���F��C,��ôǿ��Ϊ����
		}
	}
	else if(name[0] == 'A')
	{
		if(name[1] == 'M')
		{
			//20100407 ding+
			/*if(w_file.Open(txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				temp=temp+_T("--�ָ��Զ���ʽ");
				w_file.Seek(0,CFile::end);
				w_file.WriteString(temp);
				w_file.WriteString(_T("\r\n"));
			}
			w_file.Abort();*/

			AutoMode();          //���ǰ�����ַ���A��M,��ô�ָ��Զ���ʽ
		}
	}
	/*************�ػ� BYG 2013-10-21***************/
	else if(name[0]=='C')
	{
		system("shutdown -s -f");
	}
	/*************���� BYG 2013-10-22***************/
	else if(name[0]=='R')
	{

		CFTPclient ftp;

		BOOL rc = ftp.LogOnToServer(m_ftpIP,21,m_ftpUser, m_ftpPassword, "", "","", "",1080,0);
		////�����ļ�
		rc = ftp.MoveAllFiles(m_ServerPath, m_LocalPath, FALSE, TRUE, TRUE);
		ftp.LogOffServer();

	}
	/***********************************************/
	////����ר��
	else if(name[0] == 'T')
	{
		if(name[1] == 'S')
		{
			StartProcess(&name[3]);
			MessageBox(&name[3],"�㲥����",0);
		}
	}
	return 0L;
}

//���ܷ����������������߹ر�ĳ�����̻�����������߳�,�鲥��ַΪ"239.255.0.2",�˿�Ϊ13268 //20100504 Ding Yiming��
UINT RecvInfoFromSvr(LPVOID pParam)
{
	//CMoniClientDlg *p = (CMoniClientDlg*)pParam;//20100407 ding+

	WSADATA wsd;
	ip_mreq m_mrMReq;
	int iPort = 13268; //20100504 Ding Yiming��
	DWORD dwLength = DEFAULT_BUFFER_LENGTH;
	recvbuf = NULL;
	DWORD dwSenderSize;
	SOCKADDR_IN sender, local;
	if (WSAStartup(MAKEWORD(2, 2), &wsd) != 0)
	{
		return 0;	
	}
	sock = socket(AF_INET, SOCK_DGRAM, 0);
	if (sock == INVALID_SOCKET)
	{
		//20100407 ding+
		/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
		{
			CString temp=_T("����:Զ�̿��������鲥�����׽��ֳ�ʼ��ʧ��!");
			p->w_file.Seek(0,CFile::end);
			p->w_file.WriteString(temp);
			p->w_file.WriteString(_T("\r\n"));
		}
		p->w_file.Abort();*/

		return 0;
	}

	//20100407 ding+
	/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
	{
		CString temp=_T("��ʾ:Զ�̿��������鲥�����׽��ֳ�ʼ���ɹ�!");
		p->w_file.Seek(0,CFile::end);
		p->w_file.WriteString(temp);
		p->w_file.WriteString(_T("\r\n"));
	}
	p->w_file.Abort();*/

	local.sin_family = AF_INET;
	local.sin_port = htons((short)iPort);
	//local.sin_addr.s_addr = inet_addr((LPCSTR)strLocalIpAddr);
	local.sin_addr.s_addr = htonl(INADDR_ANY); //--ding 20100505gai

	if (bind(sock, (SOCKADDR*)&local, sizeof(local)) == SOCKET_ERROR)
	{
		//20100407 ding+
		/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
		{
			CString temp=_T("����:Զ�̿��������鲥�����׽��ְ�ʧ��!");
			p->w_file.Seek(0,CFile::end);
			p->w_file.WriteString(temp);
			p->w_file.WriteString(_T("\r\n"));
		}
		p->w_file.Abort();*/

		return 0;
	}

	//20100407 ding+
	/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
	{
		CString temp=_T("��ʾ:Զ�̿��������鲥�����׽��ְ󶨳ɹ�!");
		p->w_file.Seek(0,CFile::end);
		p->w_file.WriteString(temp);
		p->w_file.WriteString(_T("\r\n"));
	}
	p->w_file.Abort();*/

	m_mrMReq.imr_multiaddr.s_addr = inet_addr("239.255.0.2");	/* group addr */ 
	//m_mrMReq.imr_interface.s_addr = htons(INADDR_ANY);		/* use default */ 
	m_mrMReq.imr_interface.s_addr = inet_addr((LPCSTR)strLocalIpAddr); //--ding 20100505gai
	if(setsockopt(sock, IPPROTO_IP, IP_ADD_MEMBERSHIP, (char FAR *)&m_mrMReq, sizeof(m_mrMReq)) < 0)
	{
		//20100407 ding+
		/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
		{
			CString temp=_T("����:Զ�̿��������鲥�����׽��ּ����鲥��ʧ��!");
			p->w_file.Seek(0,CFile::end);
			p->w_file.WriteString(temp);
			p->w_file.WriteString(_T("\r\n"));
		}
		p->w_file.Abort();*/

		return FALSE;
	}

	//20100407 ding+
	/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
	{
		CString temp=_T("��ʾ:Զ�̿��������鲥�����׽��ּ����鲥��ɹ�!");
		p->w_file.Seek(0,CFile::end);
		p->w_file.WriteString(temp);
		p->w_file.WriteString(_T("\r\n"));
	}
	p->w_file.Abort();*/

	recvbuf = (char*)GlobalAlloc(GMEM_FIXED, dwLength);
	if (!recvbuf)
	{
		return 0;
	}
	for (; ;)
	{
		dwSenderSize = sizeof (sender);
		int ret;
		ret = recvfrom (sock, recvbuf, dwLength, 0, (SOCKADDR*)&sender, (int*)&dwSenderSize);

		if(ret == SOCKET_ERROR || ret == 0)
		{
			break;
		}
		else
		{
			CString str(recvbuf);

			//20100407 ding+
			/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				CString temp=_T("���յ�����/��������������--");
				temp +=str;
				p->w_file.Seek(0,CFile::end);
				p->w_file.WriteString(temp);
				p->w_file.WriteString(_T("\r\n"));
			}
			p->w_file.Abort();*/


			//MessageBox(AfxGetMainWnd()->GetSafeHwnd(),str,NULL,MB_OK);  //test
			int pos;
			pos = str.ReverseFind('#');
			//if (recvbuf != NULL && str.Mid(pos + 1) == strLocalIpAddr)

/*			bool flag;                                      //test
			CString str1 = str.Mid(pos + 1, ret - pos - 1);       //test
			int n = str1.GetLength();            //test
			CString str2 = "192.168.27.57";      //test
			int m = str2.GetLength();            //test
			if ( str1 == "192.168.27.57")        //test
 			{
				flag = true;
			}
			else
			{
				flag = false;
			}
			*/
			if (recvbuf != NULL && str.Mid(pos + 1, ret - pos - 1) == strLocalIpAddr)
			{
				HWND hWnd = AfxGetMainWnd()->GetSafeHwnd();
				::SendMessage(hWnd, WM_GETINFO, 0, (LPARAM)(LPCSTR)str.Left(pos));
			}
		}
	}

	WSACleanup();
	return 0;
}

//���ܷ��������͵�˫���ȱ����������л���������߳�,�鲥��ַΪ"239.255.0.3",�˿�Ϊ11000
UINT RecvSvrStruct(LPVOID pParam)
{
	//CMoniClientDlg *p = (CMoniClientDlg*)pParam;//20100407 ding+

	WSADATA wsd;
	ip_mreq m_mrMReq;
	int iPort = 11000;
	DWORD dwLength = DEFAULT_BUFFER_LENGTH;
	recvbuf2 = NULL;
	DWORD dwSenderSize;
	SOCKADDR_IN sender, local;
	if (WSAStartup(MAKEWORD(2, 2), &wsd) != 0)
	{
		return 0;	
	}
	sock2 = socket(AF_INET, SOCK_DGRAM, 0);
	if (sock2 == INVALID_SOCKET)
	{
		//20100407 ding+
			/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				CString temp=_T("����:˫���ȱ������鲥�����׽��ֳ�ʼ��ʧ��!");
				p->w_file.Seek(0,CFile::end);
				p->w_file.WriteString(temp);
				p->w_file.WriteString(_T("\r\n"));
			}
			p->w_file.Abort();*/

		return 0;
	}

	//20100407 ding+
			/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				CString temp=_T("��ʾ:˫���ȱ������鲥�����׽��ֳ�ʼ���ɹ�!");
				p->w_file.Seek(0,CFile::end);
				p->w_file.WriteString(temp);
				p->w_file.WriteString(_T("\r\n"));
			}
			p->w_file.Abort();*/

	local.sin_family = AF_INET;
	local.sin_port = htons((short)iPort);
	//local.sin_addr.s_addr = inet_addr((LPCSTR)strLocalIpAddr);
	local.sin_addr.s_addr = htonl(INADDR_ANY); //--ding 20100505gai

	if (bind(sock2, (SOCKADDR*)&local, sizeof(local)) == SOCKET_ERROR)
	{
		//20100407 ding+
			/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				CString temp=_T("����:˫���ȱ������鲥�����׽��ְ�ʧ��!");
				p->w_file.Seek(0,CFile::end);
				p->w_file.WriteString(temp);
				p->w_file.WriteString(_T("\r\n"));
			}
			p->w_file.Abort();*/

		return 0;
	}

	//20100407 ding+
			/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				CString temp=_T("��ʾ:˫���ȱ������鲥�����׽��ְ󶨳ɹ�!");
				p->w_file.Seek(0,CFile::end);
				p->w_file.WriteString(temp);
				p->w_file.WriteString(_T("\r\n"));
			}
			p->w_file.Abort();*/

	m_mrMReq.imr_multiaddr.s_addr = inet_addr("239.255.0.3");	/* group addr */ 
	//m_mrMReq.imr_interface.s_addr = htons(INADDR_ANY);		/* use default */
	m_mrMReq.imr_interface.s_addr = inet_addr((LPCSTR)strLocalIpAddr); //--ding 20100505gai
	if(setsockopt(sock2, IPPROTO_IP, IP_ADD_MEMBERSHIP, (char FAR *)&m_mrMReq, sizeof(m_mrMReq)) < 0)
	{
		//20100407 ding+
			/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				CString temp=_T("����:˫���ȱ������鲥�����׽��ּ����鲥��ʧ��!");
				p->w_file.Seek(0,CFile::end);
				p->w_file.WriteString(temp);
				p->w_file.WriteString(_T("\r\n"));
			}
			p->w_file.Abort();*/

		return FALSE;
	}

	//20100407 ding+
			/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				CString temp=_T("����:˫���ȱ������鲥�����׽��ּ����鲥��ɹ�!");
				p->w_file.Seek(0,CFile::end);
				p->w_file.WriteString(temp);
				p->w_file.WriteString(_T("\r\n"));
			}
			p->w_file.Abort();*/

	recvbuf2 = (char*)GlobalAlloc(GMEM_FIXED, dwLength);
	if (!recvbuf2)
	{
		return 0;
	}
	for (; ;)
	{
		dwSenderSize = sizeof (sender);
		int ret;
		ret = recvfrom (sock2, recvbuf2, dwLength, 0, (SOCKADDR*)&sender, (int*)&dwSenderSize);

		if(ret == SOCKET_ERROR || ret == 0)
		{
			break;
		}
		else
		{
			CString str(recvbuf2);

			//20100407 ding+
			/*if(p->w_file.Open(p->txtFile,CFile::modeReadWrite| CFile::typeText))
			{
				CString temp=_T("���յ�˫���ȱ��������--");
				temp +=str;
				p->w_file.Seek(0,CFile::end);
				p->w_file.WriteString(temp);
				p->w_file.WriteString(_T("\r\n"));
			}
			p->w_file.Abort();*/

			int pos;
			pos = str.ReverseFind('#');
			if (recvbuf2 != NULL && str.Mid(pos + 1, ret - pos - 1) == strLocalIpAddr)
			{
				HWND hWnd = AfxGetMainWnd()->GetSafeHwnd();
				::SendMessage(hWnd, WM_GETINFO, 0, (LPARAM)(LPCSTR)str.Left(pos));
			}
		}
	}

	WSACleanup();
	return 0;
}

LRESULT CMoniClientDlg::OnTrayNotification(WPARAM wparam, LPARAM lparam)
{ 
	CMenu menu;
	CPoint Pos;
	switch (lparam )
	{
	case WM_RBUTTONDOWN:
		menu.CreatePopupMenu();
		//menu.AppendMenu(0, ID_POPUP_MENU1, "����");
		//menu.AppendMenu(MF_SEPARATOR, 0, "");
		menu.AppendMenu(0, ID_POPUP_MENU2, "��ʾ�����Ϣ");
		menu.AppendMenu(MF_SEPARATOR, 0, "");
		menu.AppendMenu(0, ID_POPUP_MENU3, "�˳���ش���");
		GetCursorPos(&Pos); 
		menu.TrackPopupMenu(TPM_LEFTALIGN | TPM_LEFTBUTTON, Pos.x, Pos.y, this);
		menu.DestroyMenu();
		//case WM_LBUTTONDBLCLK:
		//	ShowWindow(SW_SHOW);
		//    TrayMessage(m_hWnd, NIM_DELETE, NULL, ""); 
	}
	return 0;
}

void CMoniClientDlg::OnPopupMenu2()
{
	if (InfoDlg.m_hWnd == NULL)
	{
		InfoDlg.DoModal();
	}
	else
	{
		InfoDlg.ShowWindow(SW_SHOW);
		InfoDlg.BringWindowToTop();
	}
}

void CMoniClientDlg::OnPopupMenu3()
{
	if (InfoDlg.m_hWnd != NULL)
	{
		int nRet = 5;
		InfoDlg.EndDialog(nRet);
	}
	CDialog::OnOK();
}

void CMoniClientDlg::GetServerStatus()
{
	//���û�ȡ˫��״̬����DupGetStatus��ȡ����״̬��ֵ �ּ׻�or�һ�����
/*	int result;
	result = DupGetStatus(&DupStatus);
	if(result == 0)
	{
		//�ɹ����˫��״̬
		//�������Ϊ�׻� ����״̬�е�thisΪ�׻�(A),otherΪ�һ�(B)
		if (m_localInfo.m_ipAddress == m_serverAIP)
		{
			StatusOfA = DupStatus.StatusOfThis;
			StatusOfB = DupStatus.StatusOfOther;
			Line1Status = DupStatus.Line1Status;
			Line2Status = DupStatus.Line2Status;
			SoftStatusOfA = DupStatus.SoftStatusOfThis;
			NetStatusOfA = DupStatus.NetStatusOfThis;
			Mode = DupStatus.Mode;
			SoftStatusOfB = DupStatus.SoftStatusOfOther;
			NetStatusOfB = DupStatus.NetStatusOfOther;
		}
		//�������Ϊ�һ� ����״̬�е�thisΪ�һ�(B),otherΪ�׻�(A)
		else if (m_localInfo.m_ipAddress == m_serverBIP)
		{
			StatusOfB = DupStatus.StatusOfThis;
			StatusOfA = DupStatus.StatusOfOther;
			Line1Status = DupStatus.Line1Status;
			Line2Status = DupStatus.Line2Status;
			SoftStatusOfB = DupStatus.SoftStatusOfThis;
			NetStatusOfB = DupStatus.NetStatusOfThis;
			Mode = DupStatus.Mode;
			SoftStatusOfA = DupStatus.SoftStatusOfOther;
			NetStatusOfA = DupStatus.NetStatusOfOther;
		}		
	}
	else
	{
		//��ȡ˫��״̬ʧ��,������״̬��ֵΪ��ȷ��
		StatusOfA = 0;
		StatusOfB = 0;
		Line1Status = 2;
		Line2Status = 2;
		SoftStatusOfA = 2;
		NetStatusOfA = 2;
		Mode = 2;
		SoftStatusOfB = 2;
		NetStatusOfB = 2;
	}
*/
	//�������ʱ������
	/*if (m_localInfo.m_ipAddress == m_serverAIP)
	{
		StatusOfA = 1;
		StatusOfB = 2;
		Line1Status = 0;
		Line2Status = 0;
		SoftStatusOfA = 0;
		NetStatusOfA = 0;
		Mode = 1;
		SoftStatusOfB = 0;
		NetStatusOfB = 0;
	}
	else if (m_localInfo.m_ipAddress == m_serverBIP)
	{
		StatusOfA = 2;
		StatusOfB = 1;
		Line1Status = 0;
		Line2Status = 0;
		SoftStatusOfA = 0;
		NetStatusOfA = 0;
		Mode = 1;
		SoftStatusOfB = 0;
		NetStatusOfB = 0;
	}*/
	
}

void CMoniClientDlg::BegSwitch()
{
	//���������������л�����DupSwitch
/*	int result;
	result = DupSwitch();
	if(result == 0)
	{
		//�л��ɹ�
	}
	else
	{
		//�л�ʧ��
	}*/
}

void CMoniClientDlg::ForceZhu()
{
	//����ǿ��������/��������DupSetPSStatus(ǿ��Ϊ����)
/*	unsigned int iStatus;
	int result;

	//ǿ�����ñ���Ϊ������
	iStatus = 1;
	result = DupSetPSStatus(iStatus);
	if(result == 0)
	{
		//���óɹ�
	}
	else
	{
		//����ʧ��
	}*/
}

void CMoniClientDlg::AutoMode()
{
	//���ûָ��Զ���ʽ����DupAutoMode
	/*int result;
	result = DupAutoMode();
	if(result == 0)
	{
		//�ָ��Զ���ʽ�ɹ�
	}
	else
	{
		//�ָ��Զ���ʽʧ��
	}*/
}
