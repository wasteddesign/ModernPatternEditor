#include "StdAfx.h"
#include "ActionStack.h"
#include "MachinePattern.h"
#include "Compress.h"

void CMachineDataOutput::Write(void *pbuf, int const numbytes) {}
void CMachineDataInput::Read(void *pbuf, int const numbytes) {}


CActionStack::CActionStack(void)
{
	position = 0;
	::InitializeCriticalSection(&cs);
}

CActionStack::~CActionStack(void)
{
	::DeleteCriticalSection(&cs);
}

class CMemDataOutput : public CMachineDataOutput
{
public:
	CMemDataOutput(ByteVector *p) { pdata = p; }

	virtual void Write(void *pbuf, int const numbytes)
	{
		int oldsize = (int)pdata->size();
		pdata->resize(oldsize + numbytes);
		memcpy(&(*pdata)[oldsize], pbuf, numbytes);		// try to avoid: &pdata[oldsize]
	}

	ByteVector *pdata;
};

class CMemDataInput : public CMachineDataInput
{
public:
	CMemDataInput(ByteVector *p) { pdata = p; pos = 0;}

	virtual void Read(void *pbuf, int const numbytes)
	{
		assert(pos + numbytes <= (int)pdata->size());
		memcpy(pbuf, &(*pdata)[pos], numbytes);
		pos += numbytes;
	}

	ByteVector *pdata;
	int pos;
};


void CActionStack::BeginAction(CMachinePattern *pmp, char const *name)
{
	::EnterCriticalSection(&cs);

	shared_ptr<CState> s = shared_ptr<CState>(new CState());
	s->name = name;
	SaveState(pmp, *s);
	states.resize(position++);
	states.push_back(s);
	pmp->pnd->pCB->SetModifiedFlag();

	::LeaveCriticalSection(&cs);
}

void CActionStack::Undo(CMachinePattern *pmp)
{
	if (position < 1)
		return;

	if (position == (int)states.size())
		SaveState(pmp, unmodifiedState);

	position--;
	RestoreState(pmp);
}

void CActionStack::Redo(CMachinePattern *pmp)
{
	if (position >= (int)states.size())
		return;

	position++;
	RestoreState(pmp);
}

void CActionStack::SaveState(CMachinePattern *pmp, CState &s)
{
	ByteVector uncomp;
	CMemDataOutput mdo(&uncomp);
	pmp->Write(&mdo);
	Compress(s.state, uncomp);

#ifdef _DEBUG	
	char buf[256];
	sprintf_s(buf, 256, "SaveState: compressed %d bytes, uncompressed %d bytes", s.state.size(), uncomp.size());
	pmp->pnd->pCB->WriteLine(buf);
#endif

	s.patternLength = pmp->pnd->pCB->GetPatternLength(pmp->pPattern);
}

void CActionStack::RestoreState(CMachinePattern *pmp)
{
	CState *ps;

	if (position == (int)states.size())
		ps = &unmodifiedState;
	else
		ps = states[position].get();

//	CMILock lock(pew->pCB);
	{
	
		{
			ByteVector uncomp;
			Decompress(uncomp, ps->state);

#ifdef _DEBUG
			char buf[256];
			sprintf_s(buf, 256, "RestoreState: compressed %d bytes, uncompressed %d bytes", ps->state.size(), uncomp.size());
			pmp->pnd->pCB->WriteLine(buf);
#endif

			CMemDataInput mdi(&uncomp);
//		pmp->Read(&mdi, PIANOROLL_DATA_VERSION);
			pmp->Read(&mdi);
		}

//		pmp->Init(->pCB, ps->patternLength);
		pmp->pnd->pCB->SetPatternLength(pmp->pPattern, ps->patternLength);
		//pmp->MPattern->LengthInBuzzTicks = ps->patternLength;

	}
}
