//-----------------------------------------------------------------------------
// File: task.h
// Desc: ����ִ������ִ�е�����ĳ����࣬�κ���Ҫ����ִ����ִ�е�������Ҫ��
//       �ýӿڼ̳У���ʵ��Do�ӿڣ�����ִ�����ֱ�ӵ��ø������Do�ӿ���ִ������
// Auth: Aslan
// Date: 2009-12-09
// Last: 2009-12-09
//-----------------------------------------------------------------------------
#pragma once

namespace ECore{

//-----------------------------------------------------------------------------
// ���������
//-----------------------------------------------------------------------------
class ECORE_API Task
{
public:
	//-------------------------------------------------------------------------
	// ���������
	//-------------------------------------------------------------------------
	Task();
	virtual ~Task();

	//-------------------------------------------------------------------------
	// ִ��
	//-------------------------------------------------------------------------
	virtual VOID Do()=0;
};

} // namespace ECore{