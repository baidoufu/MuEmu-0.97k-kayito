#include "stdafx.h"
#include "CommandManager.h"
#include "QueryManager.h"
#include "ServerManager.h"
#include "SocketManager.h"

CCommandManager gCommandManager;

CCommandManager::CCommandManager()
{

}

CCommandManager::~CCommandManager()
{

}

void CCommandManager::GDGlobalPostRecv(SDHP_GLOBAL_POST_RECV* lpMsg, int index)
{
	SDHP_GLOBAL_POST_SEND pMsg;

	pMsg.header.set(0x05, 0x03, sizeof(pMsg));

	pMsg.type = lpMsg->type;

	memcpy(pMsg.name, lpMsg->name, sizeof(pMsg.name));

	memcpy(pMsg.message, lpMsg->message, sizeof(pMsg.message));

	memcpy(pMsg.serverName, lpMsg->serverName, sizeof(pMsg.serverName));

	for (int n = 0; n < MAX_SERVER; n++)
	{
		if (gServerManager[n].CheckState() != 0)
		{
			gSocketManager.DataSend(n, (BYTE*)&pMsg, pMsg.header.size);
		}
	}
}

void CCommandManager::GDCommandResetRecv(SDHP_COMMAND_RESET_RECV* lpMsg, int index)
{
	SDHP_COMMAND_RESET_SEND pMsg;

	pMsg.header.set(0x05, 0x04, sizeof(pMsg));

	pMsg.index = lpMsg->index;

	memcpy(pMsg.account, lpMsg->account, sizeof(pMsg.account));

	memcpy(pMsg.name, lpMsg->name, sizeof(pMsg.name));

#if defined(SQLITE)
	// SQLite: Implement WZ_GetResetInfo logic directly
	// First, ensure ResetInfo row exists
	gQueryManager.ExecQuery("INSERT OR IGNORE INTO ResetInfo (Name) VALUES ('%s')", lpMsg->name);

	// Get Reset info from ResetInfo table
	gQueryManager.ExecQuery("SELECT ResetDay, ResetWek, ResetMon FROM ResetInfo WHERE Name='%s'", lpMsg->name);

	gQueryManager.Fetch();

	pMsg.ResetDay = gQueryManager.GetAsInteger("ResetDay");

	pMsg.ResetWek = gQueryManager.GetAsInteger("ResetWek");

	pMsg.ResetMon = gQueryManager.GetAsInteger("ResetMon");

	gQueryManager.Close();
#elif !defined(MYSQL)
	gQueryManager.ExecQuery("EXEC WZ_GetResetInfo '%s','%s'", lpMsg->account, lpMsg->name);

	gQueryManager.Fetch();

	pMsg.ResetDay = gQueryManager.GetAsInteger("ResetDay");

	pMsg.ResetWek = gQueryManager.GetAsInteger("ResetWek");

	pMsg.ResetMon = gQueryManager.GetAsInteger("ResetMon");

	gQueryManager.Close();
#else
	gQueryManager.ExecResultQuery("CALL WZ_GetResetInfo('%s', '%s')", lpMsg->account, lpMsg->name);

	gQueryManager.Fetch();

	pMsg.ResetDay = gQueryManager.GetAsInteger("ResetDay");

	pMsg.ResetWek = gQueryManager.GetAsInteger("ResetWek");

	pMsg.ResetMon = gQueryManager.GetAsInteger("ResetMon");

	gQueryManager.Close();
#endif

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void CCommandManager::GDCommandGrandResetRecv(SDHP_COMMAND_RESET_RECV* lpMsg, int index)
{
	SDHP_COMMAND_RESET_SEND pMsg;

	pMsg.header.set(0x05, 0x05, sizeof(pMsg));

	pMsg.index = lpMsg->index;

	memcpy(pMsg.account, lpMsg->account, sizeof(pMsg.account));

	memcpy(pMsg.name, lpMsg->name, sizeof(pMsg.name));

#if defined(SQLITE)
	// SQLite: Implement WZ_GetGrandResetInfo logic directly
	// First, ensure ResetInfo row exists
	gQueryManager.ExecQuery("INSERT OR IGNORE INTO ResetInfo (Name) VALUES ('%s')", lpMsg->name);

	// Get GrandReset info from ResetInfo table
	gQueryManager.ExecQuery("SELECT GrandResetDay, GrandResetWek, GrandResetMon FROM ResetInfo WHERE Name='%s'", lpMsg->name);

	gQueryManager.Fetch();

	pMsg.ResetDay = gQueryManager.GetAsInteger("GrandResetDay");

	pMsg.ResetWek = gQueryManager.GetAsInteger("GrandResetWek");

	pMsg.ResetMon = gQueryManager.GetAsInteger("GrandResetMon");

	gQueryManager.Close();
#elif !defined(MYSQL)
	gQueryManager.ExecQuery("EXEC WZ_GetGrandResetInfo '%s','%s'", lpMsg->account, lpMsg->name);

	gQueryManager.Fetch();

	pMsg.ResetDay = gQueryManager.GetAsInteger("GrandResetDay");

	pMsg.ResetWek = gQueryManager.GetAsInteger("GrandResetWek");

	pMsg.ResetMon = gQueryManager.GetAsInteger("GrandResetMon");

	gQueryManager.Close();
#else
	gQueryManager.ExecResultQuery("CALL WZ_GetGrandResetInfo('%s', '%s')", lpMsg->account, lpMsg->name);

	gQueryManager.Fetch();

	pMsg.ResetDay = gQueryManager.GetAsInteger("GrandResetDay");

	pMsg.ResetWek = gQueryManager.GetAsInteger("GrandResetWek");

	pMsg.ResetMon = gQueryManager.GetAsInteger("GrandResetMon");

	gQueryManager.Close();
#endif

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}