#pragma once

#include "MachinePattern.h"

using namespace System;
using namespace System::Windows::Controls;
using namespace System::Windows::Threading;
using namespace System::Windows::Interop;
using namespace System::Runtime::InteropServices; 
using namespace WDE::ModernPatternEditor;
using namespace BuzzGUI::Interfaces;


ref class ManagedGUI : public IGUICallbacks
{
public:
	ManagedGUI(NativeData *pnd)
	{
		pnd->TargetMac = NULL;
		this->pnd = pnd;
		Control = gcnew WDE::ModernPatternEditor::PatternEditor(this);
	}

    virtual void WriteDC(String ^text)
	{
#ifdef _DEBUG
		const char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(text);
		pnd->pCB->WriteLine(str2);
		Marshal::FreeHGlobal((System::IntPtr)(void*)str2);
#endif
	}
	
	virtual int GetPlayPosition()
	{
		return pnd->PlayPos;
	}

    virtual bool GetPlayNotesState()
	{
		return pnd->pCB->GetPlayNotesState();
	}

  
    virtual void MidiNote(int note, int velocity)
	{
		if (pnd->TargetMac == NULL)
			return;

		//pnd->pCB->SendMidiNote(pnd->TargetMac, 0, note, velocity);
		//(pnd->pMI->*pnd->recordCallback)(note, velocity);
	}

	
    virtual int GetBaseOctave()
	{
		return pnd->pCB->GetBaseOctave();
	}

    virtual bool IsMidiNoteImplemented()
	{
		return pnd->pCB->MachineImplementsFunction(pnd->TargetMac, 12, false);
	}

	virtual void GetGUINotes()
	{
	}

	virtual void GetRecordedNotes()
	{
	}

    virtual int GetStateFlags()
	{
		return pnd->pCB->GetStateFlags();
	}

	virtual bool TargetSet() { return pnd->TargetMac != NULL; }

	virtual void SetStatusBarText(int pane, String ^text)
	{
		const char *s = (char*)(void*)Marshal::StringToHGlobalAnsi(text);
		pnd->pCB->SetPatternEditorStatusText(pane, s);
		Marshal::FreeHGlobal((System::IntPtr)(void*)s);
	}

	virtual void PlayNoteEvents(IEnumerable<NoteEvent>^ notes)
	{
	}

	virtual bool IsEditorWindowVisible()
	{
		HWND hwnd = (HWND)HS->Handle.ToPointer();
		HDC hdc = ::GetDC(hwnd);
		RECT r;
		bool visible = ::GetClipBox(hdc, &r) != NULLREGION;
		::ReleaseDC(hwnd, hdc);
		return visible;
	}

	virtual String ^GetThemePath()
	{
		return gcnew String(pnd->pCB->GetThemePath());
	}

	virtual String^ GetTargetMachine()
	{
		return gcnew String(pnd->pCB->GetMachineName(pnd->TargetMac));
	}

	virtual String^ GetEditorMachine()
	{
		//const char *str = pnd->pCB->GetMachineName(pnd->ThisMac);

		return gcnew String("Modern Pattern Editor");
	}

	virtual void SetPatternEditorMachine(String^ editorMachine)
	{
		const char* s = (char*)(void*)Marshal::StringToHGlobalAnsi(editorMachine);

		HWND hwnd = (HWND)HS->Handle.ToPointer();
		hwnd = GetParent(GetParent(GetParent(hwnd)));
		if (hwnd != NULL)
		{
			HWND toolbar = GetDlgItem(hwnd, EDITOR_MAIN_TOOLBAR);
			if (toolbar != NULL)
			{
				HWND combobox = GetDlgItem(toolbar, EDITOR_MAIN_PATTERN_EDITOR);
				if (combobox != NULL)
				{
					SendMessage(combobox, CB_SELECTSTRING, 0, (LPARAM)s);

					int wParam = (EDITOR_MAIN_PATTERN_EDITOR & 0xFFFF) + ((CBN_SELENDOK & 0xFFFF) << 16);
					SendMessage(toolbar, WM_COMMAND, wParam, (LPARAM)combobox);

					wParam = (EDITOR_MAIN_PATTERN_EDITOR & 0xFFFF) + ((CBN_SELCHANGE & 0xFFFF) << 16);
					SendMessage(toolbar, WM_COMMAND, wParam, (LPARAM)combobox);

					InvalidateRect(hwnd, NULL, false);
				}
			}
		}

		Marshal::FreeHGlobal((System::IntPtr)(void*)s);
	}

	virtual void SetPatternName(String ^machine, String ^oldName, String ^newName)
	{
		const char* machineName = (char*)(void*)Marshal::StringToHGlobalAnsi(machine);
		CMachine *mac = pnd->pCB->GetMachine(machineName);
		Marshal::FreeHGlobal((System::IntPtr)(void*)machineName);

		const char* patternOldName = (char*)(void*)Marshal::StringToHGlobalAnsi(oldName);
		const char* patternNewName = (char*)(void*)Marshal::StringToHGlobalAnsi(newName);
		CPattern *pat = pnd->pCB->GetPatternByName(mac, patternOldName);
		if (pat != NULL)
		{
			pnd->pCB->SetPatternName(pat, patternNewName);
		}
		
		Marshal::FreeHGlobal((System::IntPtr)(void*)patternOldName);
		Marshal::FreeHGlobal((System::IntPtr)(void*)patternNewName);
	}

	virtual void ControlChange(String ^machine, int group, int track, int param, int value)
	{
		const char* machineName = (char*)(void*)Marshal::StringToHGlobalAnsi(machine);
		CMachine* mac = pnd->pCB->GetMachine(machineName);
		Marshal::FreeHGlobal((System::IntPtr)(void*)machineName);

		pnd->pCB->ControlChange(mac, group, track, param, value);
	}

	virtual System::IntPtr GetEditorHWND()
	{
		HWND hwnd = pnd->parent;
		hwnd = GetParent(hwnd);
		return System::IntPtr(hwnd);
	}


public:

	void SetEditorPattern(const char *name)
	{	
		Control->SetEditorPattern(gcnew String(name));
	}

	void SetTargetMachine(CMachine *pmac)
	{
		pnd->TargetMac = pmac;
		Control->TargetMachineChanged();
	}

	void CreatePattern(CPattern* p, int numrows)
	{
		String^ patternName = gcnew String(pnd->pCB->GetPatternName(p));
		Control->CreatePattern(patternName, numrows);
	}

	bool ExportMidiEvents(CPattern* p, CMachineDataOutput* pout)
	{
		String^ patternName = gcnew String(pnd->pCB->GetPatternName(p));
		cli::array<unsigned char>^ data = Control->ExportMidiEvents(patternName);
		int count = data->Length;

		if (count == 0)
			return false;

		for (int i = 0; i < count; i++)
		{
			pout->Write(data[i]);
		}
		return true;
	}

	bool ImportMidiEvents(CPattern* p, CMachineDataInput* pin)
	{
		String^ patternName = gcnew String(pnd->pCB->GetPatternName(p));

		vector<byte> midiData;

		while (true)
		{
			int time;
			pin->Read(time);
			if (time < 0)
				break;

			int mididata;
			pin->Read(mididata);

			midiData.push_back(time);
			midiData.push_back(mididata);

		}
		cli::array<unsigned char>^ data = gcnew cli::array<unsigned char>(midiData.size());
		for (int i = 0; i < midiData.size(); i++)
		{
			data[i] = midiData[i];
		}
		return Control->ImportMidiEvents(patternName, data);
	}

public:
	HwndSource ^HS;
	WDE::ModernPatternEditor::PatternEditor ^Control;
	//CMachinePattern *pEditorPattern;
	NativeData *pnd;
};


class GUI
{
public:
	gcroot<ManagedGUI ^> MGUI;
	HWND Window;

	// these wrappers are here to keep Pianoroll.cpp completely unmanaged

	void MachineDestructor()
	{
		MGUI->Control->Release();
	}


	void SetEditorPattern(const char* name)
	{
		MGUI->SetEditorPattern(name);
	}

	void SetTargetMachine(CMachine *pmac)
	{
		MGUI->SetTargetMachine(pmac);
	}
	

	void ShowHelp()
	{
		// MGUI->Control->ShowHelp();
	}

	void SetBaseOctave(int bo)
	{
		//MGUI->Control->SetBaseOctave(bo);
	}

	int GetEditorPatternPosition()
	{
		// return MGUI->Control->GetEditorPatternPosition();
		return 0;
	}

	void Update()
	{
		// MGUI->Control->Update();
	}

	void ThemeChanged()
	{
		MGUI->Control->ThemeChanged();
	}

	void SetPatternData(std::vector<byte>& myData)
	{
		int size = myData.size();
		cli::array<unsigned char> ^data = gcnew cli::array<unsigned char>(size);
		for (int i = 0; i < size; i++)
		{
			data[i] = myData[i];
		}
		
		MGUI->Control->SetPatternEditorData(data);
	}

	void SavePatternData(CMachineDataOutput* const po)
	{	
		cli::array<byte>^ data = MGUI->Control->GetPatternEditorData();
		int count = data->Length;

		//string s;
		//s.append("Save data content: ");

		for (int i = 0; i < count; i++)
		{
			po->Write(data[i]);
			//s += to_string(data[i]);
			//s.append(", ");
		}

		//MGUI->pnd->pCB->WriteLine(s.c_str());
	}

	void CreatePattern(CPattern* p, int numrows)
	{
		MGUI->CreatePattern(p, numrows);
	}

	bool ExportMidiEvents(CPattern* p, CMachineDataOutput* pout)
	{
		return MGUI->ExportMidiEvents(p, pout);
	}

	void Work(int posInTick, int posInSubTick)
	{
		SongTime^ songTime = gcnew SongTime();
		const CSubTickInfo* info = MGUI->pnd->pCB->GetSubTickInfo();
		CMasterInfo* mInfo = MGUI->pnd->pMI->pMasterInfo;
		songTime->PosInSubTick = info->PosInSubTick;
		songTime->CurrentSubTick = info->CurrentSubTick;
		songTime->SamplesPerSubTick = info->SamplesPerSubTick;
		songTime->SubTicksPerTick = info->SubTicksPerTick;
		songTime->PosInTick = mInfo->PosInTick;
		songTime->SamplesPerSec = mInfo->SamplesPerSec;
		songTime->TicksPerBeat = mInfo->TicksPerBeat;
		songTime->CurrentTick = MGUI->pnd->pCB->GetSongPosition();

		MGUI->Control->Work(songTime);
	}

	bool CanUndo()
	{	
		return MGUI->Control->CanUndo();
	}

	bool CanRedo()
	{
		return MGUI->Control->CanRedo();
	}

	void Undo()
	{
		MGUI->Control->Undo();
	}

	void Redo()
	{
		MGUI->Control->Redo();
	}

	void Cut()
	{
		MGUI->Control->DoCut();
	}

	void Copy()
	{
		MGUI->Control->DoCopy();
	}

	void Paste()
	{
		MGUI->Control->DoPaste();
	}

	void InitMachine()
	{
		MGUI->Control->InitMachine();
	}

	void CreatePatternCopy(CPattern* pnew, CPattern * pold)
	{
		String^ newName = gcnew String(MGUI->pnd->pCB->GetPatternName(pnew));
		String^ oldName = gcnew String(MGUI->pnd->pCB->GetPatternName(pold));
		MGUI->Control->CreatePatternCopy(newName, oldName);
	}

	void ShowPatternProperties()
	{
		MGUI->Control->ShowPatternProperties();
	}

	void AddTrack()
	{
		MGUI->Control->AddTrack();
	}

	void DeleteLastTrack()
	{
		MGUI->Control->DeleteLastTrack();
	}

	void RecordControlChange(CMachine* pmac, int group, int track, int param, int value)
	{
		String^ macName = gcnew String(MGUI->pnd->pCB->GetMachineName(pmac));
		MGUI->Control->RecordControlChange(macName, group, track, param, value);
	}

	void MidiNote(int const channel, int const value, int const velocity)
	{
		MGUI->Control->MidiNote(channel, value, velocity);
	}

	void MidiControlChange(int const ctrl, int const channel, int const value)
	{
		MGUI->Control->MidiControlChange(ctrl, channel, value);
	}
};

extern void CreateGUI(GUI &gui, HWND parent, NativeData *pnd);
