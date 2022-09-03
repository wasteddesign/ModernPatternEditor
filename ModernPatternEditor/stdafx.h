// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently

#pragma once

#define _SECURE_SCL 0

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN            // Exclude rarely-used stuff from Windows headers
#endif

#include "targetver.h"

#pragma unmanaged	// make all std code unmanaged

#include <windows.h>
#include <math.h>
#include <string>
#include <memory>
#include <algorithm>
#include <vector>
#include <map>
#include <set>

#include "../../buzz/MachineInterface.h"

#pragma managed

#include <vcclr.h>
#include <msclr/marshal_cppstd.h>


#define ID_EDIT_COPY                    0xE122
#define ID_EDIT_CUT                     0xE123
#define ID_EDIT_PASTE                   0xE125
#define ID_EDIT_UNDO                    0xE12B
#define ID_EDIT_REDO                    0xE12C


using namespace std;

#define PATTERNXP_DATA_VERSION			3
#define NOT_PATTERNXP_DATA  			255

#define EDITOR_MAIN_TOOLBAR	            105      // Control ID
#define EDITOR_MAIN_STATUSBAR           0xE801   // Control ID
#define EDITOR_MAIN_PATTERN_EDITOR      0x03EC   // Control ID