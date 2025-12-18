#pragma once

#include <winsock2.h>
#include <windows.h>

void StartLauncherProxy(unsigned short listenPort, unsigned short targetPort, bool strict = true);
void StopLauncherProxy();
