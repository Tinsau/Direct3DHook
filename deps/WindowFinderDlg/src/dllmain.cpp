// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "stdafx.h"

extern HINSTANCE g_hInst;
extern BOOL InitialiseResources();
extern BOOL UninitialiseResources();

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		g_hInst = hModule;
		InitialiseResources();
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:
		UninitialiseResources();
		g_hInst = NULL;
		break;
	}
	return TRUE;
}

