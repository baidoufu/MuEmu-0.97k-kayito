#include "stdafx.h"
#include "LauncherProxy.h"
#include <thread>
#include <atomic>
#include <vector>
#include <iostream>
#include "Util.h"

static std::atomic<bool> g_running(false);
static SOCKET g_listenSocket = INVALID_SOCKET;
static std::thread g_acceptThread;
static unsigned short g_targetPort = 0;
static bool g_strict = true;

static bool IsLauncherPacket(SOCKET s)
{
// Wait for data up to 5 seconds if strict, else accept if no data
fd_set readfds;
FD_ZERO(&readfds);
FD_SET(s, &readfds);
timeval tv;
tv.tv_sec = g_strict ? 5 : 1;
tv.tv_usec = 0;

	int sel = select(0, &readfds, NULL, NULL, &tv);
	if (sel <= 0) {
		if (!g_strict) {
			LogAdd(LOG_BLUE, "LauncherProxy: no initial data within timeout, accepting connection (non-strict)");
			return true;
		}
		// In strict mode, treat lack of data as unknown; accept to avoid false rejects
		LogAdd(LOG_BLUE, "LauncherProxy: no initial data within timeout, accepting connection (strict)");
		return true;
	}

	char peekBuf[4] = {0};
	int ret = recv(s, peekBuf, sizeof(peekBuf), MSG_PEEK);
	if (ret <= 0) return false;

	unsigned char first = (unsigned char)peekBuf[0];
	unsigned char head = 0;

	if (first == 0xC1)
	{
		if (ret < 3) return true; // not enough yet, accept and let normal flow handle
		head = (unsigned char)peekBuf[2];
	}
	else if (first == 0xC2)
	{
		if (ret < 4) return true; // not enough yet, accept
		head = (unsigned char)peekBuf[3];
	}
	else
	{
		// Log first bytes for debugging
		LogAdd(LOG_BLUE, "LauncherProxy: initial byte 0x%02X not C1/C2, rejecting", first);
		char dump[64] = {0};
		for (int i = 0; i < ret && i < 16; ++i) sprintf_s(dump + strlen(dump), sizeof(dump) - strlen(dump), "%02X ", (unsigned char)peekBuf[i]);
		LogAdd(LOG_BLUE, "LauncherProxy: peek data: %s", dump);
		return false;
	}

	// Launcher protocol heads 0x10 - 0x16 (we allow 0x10-0x16 as requested)
	if (head >= 0x10 && head <= 0x16) return true;

	// Log unexpected head
	LogAdd(LOG_BLUE, "LauncherProxy: packet head 0x%02X not launcher, rejecting", head);
	return false;
}

static void ClientHandler(SOCKET client)
{
// Inspect initial packet to ensure it's a launcher packet
if (!IsLauncherPacket(client))
{
	LogAdd(LOG_BLUE, "LauncherProxy: rejected non-launcher connection");
	closesocket(client);
	return;
}

// Connect to local joinserver on 127.0.0.1:g_targetPort
SOCKET srv = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
if (srv == INVALID_SOCKET) {
	closesocket(client);
	return;
}

sockaddr_in addr;
addr.sin_family = AF_INET;
addr.sin_port = htons(g_targetPort);
addr.sin_addr.s_addr = inet_addr("127.0.0.1");

if (connect(srv, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
	LogAdd(LOG_RED, "LauncherProxy: connect to 127.0.0.1:%d failed, err=%d", g_targetPort, WSAGetLastError());
	closesocket(srv);
	closesocket(client);
	return;
}

// Relay data both directions
auto relay = [](SOCKET a, SOCKET b){
	char buffer[4096];
	while (true) {
		int ret = recv(a, buffer, sizeof(buffer), 0);
		if (ret <= 0) break;
		int sent = send(b, buffer, ret, 0);
		if (sent == SOCKET_ERROR) break;
	}
	shutdown(a, SD_BOTH);
	shutdown(b, SD_BOTH);
	closesocket(a);
	closesocket(b);
};

std::thread t1(relay, client, srv);
std::thread t2(relay, srv, client);
t1.detach();
t2.detach();
}

static void AcceptLoop(unsigned short listenPort)
{
while (g_running)
{
sockaddr_in clientAddr;
int addrLen = sizeof(clientAddr);
SOCKET client = accept(g_listenSocket, (sockaddr*)&clientAddr, &addrLen);
if (client == INVALID_SOCKET) {
if (g_running) LogAdd(LOG_RED, "LauncherProxy accept failed: %d", WSAGetLastError());
break;
}

LogAdd(LOG_BLUE, "LauncherProxy: accepted connection from %s", inet_ntoa(clientAddr.sin_addr));
ClientHandler(client);
}
}

void StartLauncherProxy(unsigned short listenPort, unsigned short targetPort, bool strict)
{
if (g_running) return;
g_targetPort = targetPort;
g_strict = strict;
g_running = true;

g_listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
if (g_listenSocket == INVALID_SOCKET) {
LogAdd(LOG_RED, "LauncherProxy socket() failed: %d", WSAGetLastError());
g_running = false;
return;
}

sockaddr_in addr;
addr.sin_family = AF_INET;
addr.sin_addr.s_addr = INADDR_ANY; // listen on all interfaces
addr.sin_port = htons(listenPort);

if (bind(g_listenSocket, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
LogAdd(LOG_RED, "LauncherProxy bind() failed: %d", WSAGetLastError());
closesocket(g_listenSocket);
g_running = false;
return;
}

if (listen(g_listenSocket, SOMAXCONN) == SOCKET_ERROR) {
LogAdd(LOG_RED, "LauncherProxy listen() failed: %d", WSAGetLastError());
closesocket(g_listenSocket);
g_running = false;
return;
}

g_acceptThread = std::thread(AcceptLoop, listenPort);
g_acceptThread.detach();

LogAdd(LOG_BLUE, "LauncherProxy started on port %d -> 127.0.0.1:%d", listenPort, targetPort);
}

void StopLauncherProxy()
{
if (!g_running) return;
g_running = false;
closesocket(g_listenSocket);
LogAdd(LOG_BLUE, "LauncherProxy stopped");
}
