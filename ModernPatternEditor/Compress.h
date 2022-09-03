#pragma once

#pragma managed(push, off)

extern "C"
{
#include "liblzf/lzf.h"
}

inline void Decompress(ByteVector &out, ByteVector const &in)
{
	out.resize(*(unsigned int *)(&in[0]));
	int r = ::lzf_decompress(&in[4], (int)in.size() - 4, &out[0], (int)out.size());
	assert(r > 0);
}

inline void Compress(ByteVector &out, ByteVector const &in)
{
	int maxsize = 16 + 4 + (int)ceil((double)in.size() * 1.2);
	out.resize(maxsize);
	*(unsigned int *)(&out[0]) = (int)in.size();
	int r = ::lzf_compress(&in[0], (int)in.size(), &out[4], (int)out.size() - 4);
	assert(r > 0);
	out.resize(4 + r);

#ifdef _DEBUG
	ByteVector verify;
	Decompress(verify, out);
	assert(in.size() == verify.size());
	for (int i = 0; i < (int)in.size(); i++)
		assert(in[i] == verify[i]);
#endif
}



#pragma managed(pop)