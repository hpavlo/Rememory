#pragma once

class HashHelper
{
public:
	static BYTE* ComputeSHA256(void* data, DWORD dataLength);

private:
	HashHelper();
};
