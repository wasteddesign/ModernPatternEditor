#pragma once

class DataHelper
{
public:

	// static int ColumnIndex;
	// This simply raeds saved pxp data to byte buffer
	static void ReadData(std::vector<byte> &myData, CMachineDataInput* const pi)
	{
		byte version;
		pi->Read(version);
		myData.push_back(version);

		if (version == NOT_PATTERNXP_DATA)
		{
			pi->Read(version);
			myData.push_back(version);
			int dataSize = 0;
			pi->Read(dataSize);
			PushInt(myData, dataSize);

			dataSize = dataSize - sizeof(byte) - sizeof(byte) - sizeof(int);
			//dataSize = dataSize - 1 - 1 - 4;

			for (int i = 0; i < dataSize; i++)
			{
				byte b;
				pi->Read(b);
				myData.push_back(b);
			}

			return;
		}
		else
		{
			int numpat;
			pi->Read(numpat);
			PushInt(myData, numpat);

			for (int i = 0; i < numpat; i++)
			{
				string name = ReadString(myData, pi);

				// read patternp->Read(pi);
				//loadedPatterns[name] = p;
				ReadPattern(myData, pi, version);
			}
		}
	}

	static void ReadPattern(std::vector<byte>& myData, CMachineDataInput* const pi, byte ver)
	{
		int rowsPerBeat;
		if (ver > 1)
		{
			pi->Read(rowsPerBeat);
			PushInt(myData, rowsPerBeat);
		}
		
		int count;
		pi->Read(count);
		PushInt(myData, count);

		//ColumnIndex = 0;

		for (int i = 0; i < count; i++)
		{
			//auto pc = make_shared<CColumn>();
			//pc->Read(pi, ver);
			//columns.push_back(pc);
			ReadColumn(myData, pi, ver);
		}

	}

	static void ReadColumn(std::vector<byte>& myData, CMachineDataInput* const pi, byte ver)
	{
		string machineName = ReadString(myData, pi);
		int paramIndex;
		pi->Read(paramIndex);
		PushInt(myData, paramIndex);

		int paramTrack;
		pi->Read(paramTrack);
		PushInt(myData, paramTrack);

		bool graphical = false;

		if (ver >= 3)
		{
			pi->Read(graphical);
			myData.push_back(graphical);
		}

		int count; // Event count
		pi->Read(count);
		PushInt(myData, count);

		for (int i = 0; i < count; i++)
		{
			int first;
			pi->Read(first);
			PushInt(myData, first);

			int second;
			pi->Read(second);
			PushInt(myData, second);
		}

	}

	static string ReadString(std::vector<byte>& myData, CMachineDataInput* const pi)
	{
		string name;

		while (true)
		{
			char ch;
			pi->Read(ch);
			if (ch == 0)
				break;
			name += ch;
			myData.push_back(ch);
		}
		myData.push_back(0);

		return name;
	}

	static void PushInt(std::vector<byte>& myData, int value)
	{
		myData.push_back(value);
		myData.push_back(value >> 8);
		myData.push_back(value >> 16);
		myData.push_back(value >> 24);
	}
};