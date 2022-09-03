#include "stdafx.h"
#include "MPEGUI.h"
#include "DataHelper.h"

#pragma unmanaged

//#define CHECK_BUILD_NUMBER

#ifdef CHECK_BUILD_NUMBER
static char const *BuildNumber =
#include "../../buildcount"
;
#endif

#define PIANOROLL_TPB		960

CMachineParameter const paraDummy = 
{ 
	pt_byte,										// type
	"Dummy",
	"Dummy",							// description
	0,												// MinValue	
	127,											// MaxValue
	255,												// NoValue
	MPF_STATE,										// Flags
	0
};


static CMachineParameter const *pParameters[] = { 
	// track
	&paraDummy,
};

#pragma pack(1)

class gvals
{
public:
	byte dummy;

};

#pragma pack()

CMachineInfo const MacInfo = 
{
	MT_GENERATOR,							// type
	MI_VERSION,
	MIF_PATTERN_EDITOR | MIF_NO_OUTPUT | MIF_CONTROL_MACHINE | MIF_PE_NO_CLIENT_EDGE,						// flags	
	0,											// min tracks
	0,								// max tracks
	1,										// numGlobalParameters
	0,										// numTrackParameters
	pParameters,
	0, 
	NULL,
	"Modern Pattern Editor",
	"MPE",								// short name
	"WDE", 						// author
	NULL
};

class mi;

class miex : public CMachineInterfaceEx
{
public:
	virtual void* CreatePatternEditor(void* parenthwnd);
	virtual void CreatePattern(CPattern* p, int numrows);
	virtual void CreatePatternCopy(CPattern* pnew, CPattern const* pold);
	virtual void DeletePattern(CPattern* p);
	virtual void RenamePattern(CPattern* p, char const* name);
	virtual void SetPatternLength(CPattern* p, int length);
	virtual void PlayPattern(CPattern* p, CSequence* s, int offset);
	virtual void SetEditorPattern(CPattern* p);
	virtual void SetPatternTargetMachine(CPattern* p, CMachine* pmac);
	virtual bool ShowPatternEditorHelp();
	virtual bool ShowPatternProperties();
	virtual void SetBaseOctave(int bo);
	virtual bool EnableCommandUI(int id);
	virtual int GetEditorPatternPosition();
	virtual void GotMidiFocus();
	virtual void LostMidiFocus();
	virtual void MidiControlChange(int const ctrl, int const channel, int const value);
	virtual void ThemeChanged();
	virtual void AddTrack();
	virtual void DeleteLastTrack();
	virtual void RecordControlChange(CMachine* pmac, int group, int track, int param, int value);

	bool ExportMidiEvents(CPattern* p, CMachineDataOutput* pout);
	bool ImportMidiEvents(CPattern* p, CMachineDataInput* pin);

public:
	mi* pmi;

};
class mi : public CMachineInterface
{
public:
	mi();
	virtual ~mi();

	virtual void Init(CMachineDataInput* const pi);
	virtual void Tick();
	virtual bool Work(float* pout, int numsamples, int const mode);
	virtual void Save(CMachineDataOutput* const po);
	virtual void MidiNote(int const channel, int const value, int const velocity);

	bool DClickMachine(void*)
	{
		pCB->SetPatternEditorMachine(ThisMac, true);
		return true;
	}

	virtual void Stop();

	void StopPlayingNotes();

	void AddToRecBuffer(int key, int vel);

	void GotMidiFocus();
	void LostMidiFocus();

private:


public:
	miex ex;
	CMachine* ThisMac;
	CMachine* TargetMac;
	CPattern* PlayingPattern;
	int patternPos;
	int posInTick;
	NativeData nd;
	CRITICAL_SECTION amnCS;
	bool recording;
	vector<byte> machineData;

	GUI gui;
	gvals gval;

};

void miex::GotMidiFocus() { pmi->GotMidiFocus(); }
void miex::LostMidiFocus() { pmi->LostMidiFocus(); }

void miex::AddTrack() { pmi->gui.AddTrack(); }
void miex::DeleteLastTrack() { pmi->gui.DeleteLastTrack(); }

bool miex::ExportMidiEvents(CPattern* p, CMachineDataOutput* pout) { return pmi->gui.ExportMidiEvents(p, pout); }
bool miex::ImportMidiEvents(CPattern* p, CMachineDataInput* pin) { return false; /* pmi->patterns[p]->ImportMidiEvents(pin); */ }

void miex::ThemeChanged() { pmi->gui.ThemeChanged(); }

DLL_EXPORTS

extern void SetResolveEventHandler();

mi::mi()
{
	ex.pmi = this;
	GlobalVals = &gval;
	TrackVals = NULL;
	AttrVals = NULL;
	PlayingPattern = NULL;
	PlayingPattern = NULL;
	TargetMac = NULL;
	recording = false;
	nd.parent = NULL;
	::InitializeCriticalSection(&nd.PatternCS);
	::InitializeCriticalSection(&amnCS);

	SetResolveEventHandler();
}

mi::~mi()
{
	::EnterCriticalSection(&amnCS);
	if (gui.Window != NULL)
	{
		gui.MachineDestructor();
		::DestroyWindow(gui.Window);
	}
	::LeaveCriticalSection(&amnCS);

	::DeleteCriticalSection(&nd.PatternCS);
	::DeleteCriticalSection(&amnCS);
}

void mi::Init(CMachineDataInput* const pi)
{
	nd.pMI = this;
	nd.pCB = pCB;
	nd.PlayPos = -1;

	pCB->SetMachineInterfaceEx(&ex);
	ThisMac = pCB->GetThisMachine();
	pCB->SetEventHandler(ThisMac, DoubleClickMachine, (EVENT_HANDLER_PTR)&mi::DClickMachine, NULL);
	nd.ThisMac = ThisMac;

	if (pi != NULL)
	{
		::EnterCriticalSection(&amnCS);
		DataHelper::ReadData(machineData, pi);
		::LeaveCriticalSection(&amnCS);
	}
}

void mi::Save(CMachineDataOutput* const po)
{
	::EnterCriticalSection(&amnCS);
	gui.SavePatternData(po);
	::LeaveCriticalSection(&amnCS);
}


void* miex::CreatePatternEditor(void* parenthwnd)
{	
	pmi->nd.parent = (HWND)parenthwnd;
	::EnterCriticalSection(&pmi->amnCS);
	pmi->gui.Window = 0;
	CreateGUI(pmi->gui, (HWND)parenthwnd, &pmi->nd);

	pmi->gui.SetPatternData(pmi->machineData);
	pmi->gui.InitMachine();
	::LeaveCriticalSection(&pmi->amnCS);
	return pmi->gui.Window;
}

bool miex::ShowPatternProperties()
{
	pmi->gui.ShowPatternProperties();
	return true;
}

void miex::CreatePattern(CPattern* p, int numrows)
{
}

void miex::CreatePatternCopy(CPattern* pnew, CPattern const* pold)
{	
	pmi->gui.CreatePatternCopy(pnew, (CPattern *)pold);
}

void miex::DeletePattern(CPattern* p)
{
}

void miex::RenamePattern(CPattern* p, char const* name)
{
	// this is only needed if you want to display the name
}

void miex::SetPatternLength(CPattern* p, int length)
{	
}

void miex::SetEditorPattern(CPattern* p)
{
	if (p != NULL)
	{	
		pmi->gui.SetEditorPattern(pmi->pCB->GetPatternName(p));
	}
}

void miex::SetPatternTargetMachine(CPattern* p, CMachine* pmac)
{
	::EnterCriticalSection(&pmi->amnCS);
	if (p != NULL && pmac != NULL)
	{
		pmi->TargetMac = pmac;
		pmi->gui.SetTargetMachine(pmac);
	}
	::LeaveCriticalSection(&pmi->amnCS);
}

bool miex::ShowPatternEditorHelp()
{
	pmi->gui.ShowHelp();
	return true;
}

void miex::SetBaseOctave(int bo)
{
	// pmi->gui.SetBaseOctave(bo);
}

int miex::GetEditorPatternPosition()
{
	return 0;// pmi->gui.GetEditorPatternPosition();
}

void miex::PlayPattern(CPattern* p, CSequence* s, int offset)
{
	pmi->PlayingPattern = p;
}

bool miex::EnableCommandUI(int id)
{

	switch (id)
	{
	case ID_EDIT_UNDO: return pmi->gui.CanUndo(); break;
	case ID_EDIT_REDO: return pmi->gui.CanRedo(); break;
	case ID_EDIT_CUT: return true; break;
	case ID_EDIT_COPY: return true; break;
	case ID_EDIT_PASTE: return true; break;
	}
	return true;
}


void mi::MidiNote(int const channel, int const value, int const velocity)
{
	if (TargetMac == NULL)
		return;
	//::EnterCriticalSection(&amnCS);
	gui.MidiNote(channel, value, velocity);
	//::LeaveCriticalSection(&amnCS);
}

void miex::MidiControlChange(int const ctrl, int const channel, int const value)
{
	if (pmi->TargetMac == NULL)
		return;

	//::EnterCriticalSection(&pmi->amnCS);
	pmi->gui.MidiControlChange(ctrl, channel, value);
	//::LeaveCriticalSection(&pmi->amnCS);
}

void miex::RecordControlChange(CMachine* pmac, int group, int track, int param, int value)
{
	//::EnterCriticalSection(&pmi->amnCS);
	pmi->gui.RecordControlChange(pmac, group, track, param, value);
	//::LeaveCriticalSection(&pmi->amnCS);
}

void mi::GotMidiFocus()
{
}

void mi::LostMidiFocus()
{
}

void mi::AddToRecBuffer(int key, int vel)
{
}


void mi::Stop()
{
}


void mi::Tick()
{
	return;
}

void mi::StopPlayingNotes()
{
}

bool mi::Work(float* pout, int numsamples, int const mode)
{
	CSubTickInfo const* psti = pCB->GetSubTickInfo();
	if (psti != NULL && ThisMac != NULL && PlayingPattern != NULL)
	{
		::EnterCriticalSection(&amnCS);
		gui.Work(this->pMasterInfo->PosInTick, pCB->GetSubTickInfo()->PosInSubTick);
		::LeaveCriticalSection(&amnCS);
	}
	return false;
}
