#pragma once
//#include "ActionStack.h"

using namespace System::Collections::Generic;
using namespace System;
using namespace WDE::ModernPatternEditor;

#pragma unmanaged

struct NativeData
{
	CMachineInterface* pMI;
	CMICallbacks* pCB;
	CMachine* TargetMac;
	CMachine* ThisMac;
	int PlayPos;
	CRITICAL_SECTION PatternCS;
	HWND parent;
};


#pragma managed
