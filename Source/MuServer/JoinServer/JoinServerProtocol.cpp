#include "stdafx.h"
#include "JoinServerProtocol.h"
#include "MD5.h"
#include "AccountManager.h"
#include "Log.h"
#include "QueryManager.h"
#include "ServerManager.h"
#include "SocketManager.h"
#include "SocketManagerUdp.h"
#include "Util.h"

// Add local storage for command config read from GameServer data file
static int g_JS_CommandResetLevel[4] = { 0 };
static int g_JS_CommandGrandResetLevel[4] = { 0 };
static int g_JS_CommandGrandResetReset[4] = { 0 };
static int ClearPK = 0;
static bool g_JS_CommandInfoLoaded = false;

static void JS_LoadCommandInfo()
{
    if (g_JS_CommandInfoLoaded != false)
    {
        return;
    }

    const char* section = "GameServerInfo";
    const char* path = "..\\GameServer\\DATA\\GameServerInfo - Command.dat"; // relative to JoinServer

    g_JS_CommandResetLevel[0] = GetPrivateProfileInt(section, "CommandResetLevel_AL0", 0, path);
    g_JS_CommandResetLevel[1] = GetPrivateProfileInt(section, "CommandResetLevel_AL1", 0, path);
    g_JS_CommandResetLevel[2] = GetPrivateProfileInt(section, "CommandResetLevel_AL2", 0, path);
    g_JS_CommandResetLevel[3] = GetPrivateProfileInt(section, "CommandResetLevel_AL3", 0, path);

    g_JS_CommandGrandResetLevel[0] = GetPrivateProfileInt(section, "CommandGrandResetLevel_AL0", 0, path);
    g_JS_CommandGrandResetLevel[1] = GetPrivateProfileInt(section, "CommandGrandResetLevel_AL1", 0, path);
    g_JS_CommandGrandResetLevel[2] = GetPrivateProfileInt(section, "CommandGrandResetLevel_AL2", 0, path);
    g_JS_CommandGrandResetLevel[3] = GetPrivateProfileInt(section, "CommandGrandResetLevel_AL3", 0, path);

    g_JS_CommandGrandResetReset[0] = GetPrivateProfileInt(section, "CommandGrandResetReset_AL0", 0, path);
    g_JS_CommandGrandResetReset[1] = GetPrivateProfileInt(section, "CommandGrandResetReset_AL1", 0, path);
    g_JS_CommandGrandResetReset[2] = GetPrivateProfileInt(section, "CommandGrandResetReset_AL2", 0, path);
    g_JS_CommandGrandResetReset[3] = GetPrivateProfileInt(section, "CommandGrandResetReset_AL3", 0, path);

	ClearPK = GetPrivateProfileInt(section, "ClearPK", 0, path);
    g_JS_CommandInfoLoaded = true;
}

void JoinServerProtocolCore(int index, BYTE head, BYTE* lpMsg, int size)
{
    ConsoleProtocolLog(CON_PROTO_TCP_RECV, lpMsg, size);

    gServerManager[index].m_PacketTime = GetTickCount();

    switch (head)
    {
        case 0x00:
        {
            GJServerInfoRecv((SDHP_SERVER_INFO_RECV*)lpMsg, index);

            break;
        }

        case 0x01:
        {
            GJConnectAccountRecv((SDHP_CONNECT_ACCOUNT_RECV*)lpMsg, index);

            break;
        }

        case 0x02:
        {
            GJDisconnectAccountRecv((SDHP_DISCONNECT_ACCOUNT_RECV*)lpMsg, index);

            break;
        }

        case 0x03:
        {
            GJAccountLevelRecv((SDHP_ACCOUNT_LEVEL_RECV*)lpMsg, index);

            break;
        }

        case 0x04:
        {
            GJAccountLevelSaveRecv((SDHP_ACCOUNT_LEVEL_SAVE_RECV*)lpMsg, index);

            break;
        }

        case 0x06:
        {
            GJServerUserInfoRecv((SDHP_SERVER_USER_INFO_RECV*)lpMsg, index);

            break;
        }

        case 0x07:
        {
            GJAccountCountRecv((SDHP_COUNT_ONLINE_USER_RECV*)lpMsg, index);

            break;
        }

        case 0x08:
        {
            GJRegisterAccountRecv((SDHP_REGISTER_ACCOUNT_RECV*)lpMsg, index);

            break;
        }

        // Launcher protocols (0x10-0x16)
        case 0x10://登录
        {
            GJLauncherLoginRecv((SDHP_LAUNCHER_LOGIN_RECV*)lpMsg, index);

            break;
        }

        case 0x11://获取角色列表
        {
            GJLauncherGetCharactersRecv((SDHP_LAUNCHER_GET_CHARACTERS_RECV*)lpMsg, index);

            break;
        }

        case 0x12://加点
        {
            GJLauncherAddPointsRecv((SDHP_LAUNCHER_ADD_POINTS_RECV*)lpMsg, index);

            break;
        }

        case 0x13://清除Pk值
        {
            GJLauncherClearPKRecv((SDHP_LAUNCHER_CLEAR_PK_RECV*)lpMsg, index);

            break;
        }

        case 0x14://转生
        {
            GJLauncherResetRecv((SDHP_LAUNCHER_RESET_RECV*)lpMsg, index);

            break;
        }

        case 0x15://转世
        {
            GJLauncherGrandResetRecv((SDHP_LAUNCHER_GRAND_RESET_RECV*)lpMsg, index);

            break;
        }

        case 0x16://在线检查
        {
            GJLauncherCheckOnlineRecv((SDHP_LAUNCHER_CHECK_ONLINE_RECV*)lpMsg, index);

            break;
        }

        case 0x17://注册
        {
            GJLauncherRegisterRecv((SDHP_LAUNCHER_REGISTER_RECV*)lpMsg, index);

            break;
        }
    }
}

void JoinServerLiveProc()
{
	SDHP_JOIN_SERVER_LIVE_SEND pMsg;

	pMsg.header.set(0x02, sizeof(pMsg));

	pMsg.QueueSize = gSocketManager.GetQueueSize();

	gSocketManagerUdp.DataSend((BYTE*)&pMsg, pMsg.header.size);
}

void GJServerInfoRecv(SDHP_SERVER_INFO_RECV* lpMsg, int index)
{
	gServerManager[index].SetServerInfo(lpMsg->ServerName, lpMsg->ServerPort, lpMsg->ServerCode);
}

void GJConnectAccountRecv(SDHP_CONNECT_ACCOUNT_RECV* lpMsg, int index)
{
	SDHP_CONNECT_ACCOUNT_SEND pMsg;

	pMsg.header.set(0x01, sizeof(pMsg));

	pMsg.index = lpMsg->index;

	memcpy(pMsg.account, lpMsg->account, sizeof(pMsg.account));

	pMsg.result = 1;

	if (CheckTextSyntax(lpMsg->account, sizeof(lpMsg->account)) == false)
	{
		pMsg.result = 2;

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	ACCOUNT_INFO AccountInfo;

	if (gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) != false)
	{
		pMsg.result = 3;

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		JGAccountAlreadyConnectedSend(AccountInfo.GameServerCode, AccountInfo.UserIndex, AccountInfo.Account);

		return;
	}

	if (gAccountManager.GetAccountCount() >= MAX_ACCOUNT)
	{
		pMsg.result = 4;

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	if (MD5Encryption == 0)
	{
	#if defined(SQLITE)
		if (gQueryManager.ExecQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
	#elif !defined(MYSQL)
		if (gQueryManager.ExecQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s' COLLATE Latin1_General_BIN", lpMsg->account) == false || gQueryManager.Fetch() == SQL_NO_DATA)
	#else
		if (gQueryManager.ExecResultQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
	#endif
		{
			gQueryManager.Close();

			pMsg.result = 2;

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}

		char password[11] = { 0 };

		gQueryManager.GetAsString("memb__pwd", password, sizeof(password));

		if (strcmp(lpMsg->password, password) != 0 && strcmp(lpMsg->password, GlobalPassword) != 0)
		{
			gQueryManager.Close();

			pMsg.result = 0;

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}

		gQueryManager.Close();

		if (strcmp(lpMsg->password, GlobalPassword) == 0)
		{
			LogAdd(LOG_RED, "IP [%s] 使用通用密码登录了账号 '%s'!", lpMsg->IpAddress, lpMsg->account);
		}
	}
	else
	{
	#if defined(SQLITE)
		if (gQueryManager.ExecQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
	#elif !defined(MYSQL)
		if (gQueryManager.ExecQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s' COLLATE Latin1_General_BIN", lpMsg->account) == false || gQueryManager.Fetch() == SQL_NO_DATA)
	#else
		if (gQueryManager.ExecResultQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
	#endif
		{
			gQueryManager.Close();

			pMsg.result = 2;

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}

		BYTE password[16] = { 0 };

		gQueryManager.GetAsBinary("memb__pwd", password, sizeof(password));

		MD5 MD5Hash;

		if (((MD5Encryption == 1)
		    ? MD5Hash.MD5_CheckValue(lpMsg->password, (char*)password, MakeAccountKey(lpMsg->account)) == false
		    : MD5Hash.MD5_CheckValue(lpMsg->password, (char*)password) == false)
		    && strcmp(lpMsg->password, GlobalPassword) != 0)
		{
			gQueryManager.Close();

			pMsg.result = 0;

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}

		gQueryManager.Close();

		if (strcmp(lpMsg->password, GlobalPassword) == 0)
		{
			LogAdd(LOG_RED, "IP [%s] 使用通用密码登录了账号 '%s'!", lpMsg->IpAddress, lpMsg->account);
		}
	}

#if defined(SQLITE)
	// SQLite: Implement WZ_DesblocAccount logic directly
	gQueryManager.ExecQuery("UPDATE MEMB_INFO SET bloc_code=0 WHERE memb___id='%s'", lpMsg->account);
	gQueryManager.Close();

	// SQLite: Implement WZ_DesblocCharacters logic directly
	gQueryManager.ExecQuery("UPDATE Character SET CtlCode=0 WHERE AccountID='%s'", lpMsg->account);
	gQueryManager.Close();

#elif !defined(MYSQL)

	gQueryManager.ExecQuery("EXEC WZ_DesblocAccount '%s'", lpMsg->account);

	gQueryManager.Close();

	gQueryManager.ExecQuery("EXEC WZ_DesblocCharacters '%s'", lpMsg->account);

	gQueryManager.Close();

#else

	gQueryManager.ExecUpdateQuery("CALL WZ_DesblocAccount('%s')", lpMsg->account);

	gQueryManager.Close();

	gQueryManager.ExecUpdateQuery("CALL WZ_DesblocCharacters('%s')", lpMsg->account);

	gQueryManager.Close();

#endif

#if defined(SQLITE)
	if (gQueryManager.ExecQuery("SELECT sno__numb, bloc_code FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("SELECT sno__numb, bloc_code FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == SQL_NO_DATA)
#else
	if (gQueryManager.ExecResultQuery("SELECT sno__numb, bloc_code FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
#endif
	{
		gQueryManager.Close();

		pMsg.result = 2;

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	gQueryManager.GetAsString("sno__numb", pMsg.PersonalCode, sizeof(pMsg.PersonalCode));

	pMsg.BlockCode = (BYTE)gQueryManager.GetAsInteger("bloc_code");

	gQueryManager.Close();

#if defined(SQLITE)
	// SQLite: Implement WZ_GetAccountLevel logic directly
	if (gQueryManager.ExecQuery("SELECT AccountLevel, AccountExpireDate FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("EXEC WZ_GetAccountLevel '%s'", lpMsg->account) == false || gQueryManager.Fetch() == SQL_NO_DATA)
#else
	if (gQueryManager.ExecResultQuery("CALL WZ_GetAccountLevel('%s')", lpMsg->account) == false || gQueryManager.Fetch() == false)
#endif
	{
		gQueryManager.Close();

		pMsg.result = 2;

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	pMsg.AccountLevel = gQueryManager.GetAsInteger("AccountLevel");

	gQueryManager.GetAsString("AccountExpireDate", pMsg.AccountExpireDate, sizeof(pMsg.AccountExpireDate));

	gQueryManager.Close();

#if defined(SQLITE)
	// SQLite: Implement WZ_CONNECT_MEMB logic directly
	// First try to update existing MEMB_STAT record
	gQueryManager.ExecQuery("UPDATE MEMB_STAT SET ServerName='%s', IP='%s', ConnectStat=1, ConnectTM=datetime('now') WHERE memb___id='%s'", gServerManager[index].m_ServerName, lpMsg->IpAddress, lpMsg->account);
	// If no row was updated, insert a new record
	gQueryManager.ExecQuery("INSERT OR IGNORE INTO MEMB_STAT (memb___id, ServerName, IP, ConnectStat, ConnectTM) VALUES ('%s', '%s', '%s', 1, datetime('now'))", lpMsg->account, gServerManager[index].m_ServerName, lpMsg->IpAddress);
#elif !defined(MYSQL)
	gQueryManager.ExecQuery("EXEC WZ_CONNECT_MEMB '%s','%s','%s'", lpMsg->account, gServerManager[index].m_ServerName, lpMsg->IpAddress);
#else
	gQueryManager.ExecUpdateQuery("CALL WZ_CONNECT_MEMB('%s', '%s', '%s')", lpMsg->account, gServerManager[index].m_ServerName, lpMsg->IpAddress);
#endif

	gQueryManager.Close();

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

	strcpy_s(AccountInfo.Account, lpMsg->account);

	strcpy_s(AccountInfo.IpAddress, lpMsg->IpAddress);

	AccountInfo.UserIndex = lpMsg->index;

	AccountInfo.GameServerCode = gServerManager[index].m_ServerCode;

	gAccountManager.InsertAccountInfo(AccountInfo);

	gLog.Output(LOG_ACCOUNT, "[AccountInfo] Account connected (Account: %s, IpAddress: %s, GameServerCode: %d)", AccountInfo.Account, AccountInfo.IpAddress, AccountInfo.GameServerCode);
}

void GJDisconnectAccountRecv(SDHP_DISCONNECT_ACCOUNT_RECV* lpMsg, int index)
{
	SDHP_DISCONNECT_ACCOUNT_SEND pMsg;

	pMsg.header.set(0x02, sizeof(pMsg));

	pMsg.index = lpMsg->index;

	memcpy(pMsg.account, lpMsg->account, sizeof(pMsg.account));

	pMsg.result = 1;

	ACCOUNT_INFO AccountInfo;

	if (gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) == false)
	{
		pMsg.result = 0;

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	if (AccountInfo.UserIndex != lpMsg->index)
	{
		pMsg.result = 0;

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	if (AccountInfo.GameServerCode != gServerManager[index].m_ServerCode)
	{
		pMsg.result = 0;

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

#if defined(SQLITE)
	// SQLite: Implement WZ_DISCONNECT_MEMB logic directly
	gQueryManager.ExecQuery("UPDATE MEMB_STAT SET ConnectStat=0, DisConnectTM=datetime('now') WHERE memb___id='%s'", lpMsg->account);
#elif !defined(MYSQL)
	gQueryManager.ExecQuery("EXEC WZ_DISCONNECT_MEMB '%s'", lpMsg->account);
#else
	gQueryManager.ExecUpdateQuery("CALL WZ_DISCONNECT_MEMB('%s')", lpMsg->account);
#endif

	gQueryManager.Close();

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

	gAccountManager.RemoveAccountInfo(AccountInfo);

	gLog.Output(LOG_ACCOUNT, "[AccountInfo] Account disconnected (Account: %s, IpAddress: %s, GameServerCode: %d)", AccountInfo.Account, AccountInfo.IpAddress, AccountInfo.GameServerCode);
}

void GJAccountLevelRecv(SDHP_ACCOUNT_LEVEL_RECV* lpMsg, int index)
{
	ACCOUNT_INFO AccountInfo;

	if (gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) == false)
	{
		return;
	}

	SDHP_ACCOUNT_LEVEL_SEND pMsg;

	pMsg.header.set(0x03, sizeof(pMsg));

	pMsg.index = lpMsg->index;

	memcpy(pMsg.account, lpMsg->account, sizeof(pMsg.account));

#if defined(SQLITE)
	// SQLite: Implement WZ_GetAccountLevel logic directly
	if (gQueryManager.ExecQuery("SELECT AccountLevel, AccountExpireDate FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("EXEC WZ_GetAccountLevel '%s'", lpMsg->account) == false || gQueryManager.Fetch() == SQL_NO_DATA)
#else
	if (gQueryManager.ExecResultQuery("CALL WZ_GetAccountLevel('%s')", lpMsg->account) == false || gQueryManager.Fetch() == false)
#endif
	{
		gQueryManager.Close();

		pMsg.AccountLevel = 0;
	}
	else
	{
		pMsg.AccountLevel = gQueryManager.GetAsInteger("AccountLevel");

		gQueryManager.GetAsString("AccountExpireDate", pMsg.AccountExpireDate, sizeof(pMsg.AccountExpireDate));

		gQueryManager.Close();
	}

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJAccountLevelSaveRecv(SDHP_ACCOUNT_LEVEL_SAVE_RECV* lpMsg, int index)
{
#if defined(SQLITE)
	// SQLite: Implement WZ_SetAccountLevel logic directly
	gQueryManager.ExecQuery("UPDATE MEMB_INFO SET AccountLevel=%d, AccountExpireDate=datetime('now', '+%d days') WHERE memb___id='%s'", lpMsg->AccountLevel, lpMsg->AccountExpireTime, lpMsg->account);

#elif !defined(MYSQL)

	gQueryManager.ExecQuery("EXEC WZ_SetAccountLevel '%s','%d','%d'", lpMsg->account, lpMsg->AccountLevel, lpMsg->AccountExpireTime);

	gQueryManager.Fetch();

#else

	gQueryManager.ExecUpdateQuery("CALL WZ_SetAccountLevel('%s', '%d', '%d')", lpMsg->account, lpMsg->AccountLevel, lpMsg->AccountExpireTime);

#endif

	gQueryManager.Close();
}

void GJServerUserInfoRecv(SDHP_SERVER_USER_INFO_RECV* lpMsg, int index)
{
	gServerManager[index].m_CurUserCount = lpMsg->CurUserCount;

	gServerManager[index].m_MaxUserCount = lpMsg->MaxUserCount;
}

void JGAccountAlreadyConnectedSend(int GameServerCode, int UserIndex, char* account)
{
	CServerManager* lpServerManager = FindServerByCode(GameServerCode);

	if (lpServerManager == 0)
	{
		return;
	}

	SDHP_ACCOUNT_ALREADY_CONNECTED_SEND pMsg;

	pMsg.header.set(0x05, sizeof(pMsg));

	pMsg.index = UserIndex;

	memcpy(pMsg.account, account, sizeof(pMsg.account));

	gSocketManager.DataSend(lpServerManager->m_index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJAccountCountRecv(SDHP_COUNT_ONLINE_USER_RECV* lpMsg, int index)
{
	SDHP_COUNT_ONLINE_USER_SEND pMsg;

	pMsg.header.set(0x07, sizeof(pMsg));

	pMsg.index = lpMsg->index;

	pMsg.count = (int)gAccountManager.GetAccountCount();

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJRegisterAccountRecv(SDHP_REGISTER_ACCOUNT_RECV* lpMsg, int index)
{
	SDHP_REGISTER_ACCOUNT_SEND pMsg;

	pMsg.header.set(0x08, sizeof(pMsg));

	pMsg.index = lpMsg->index;

	pMsg.result = 1; // Success by default

	// Validate account name
	if (CheckTextSyntax(lpMsg->account, sizeof(lpMsg->account)) == false)
	{
		pMsg.result = 2; // Invalid account name

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Validate password
	if (CheckTextSyntax(lpMsg->password, sizeof(lpMsg->password)) == false)
	{
		pMsg.result = 2; // Invalid password

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Check if account already exists
#if defined(SQLITE)
	if (gQueryManager.ExecQuery("SELECT memb___id FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("SELECT memb___id FROM MEMB_INFO WHERE memb___id='%s' COLLATE Latin1_General_BIN", lpMsg->account) != false && gQueryManager.Fetch() != SQL_NO_DATA)
#else
	if (gQueryManager.ExecResultQuery("SELECT memb___id FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != false)
#endif
	{
		gQueryManager.Close();

		pMsg.result = 0; // Account already exists

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	gQueryManager.Close();

	// Prepare password (encode with MD5 if enabled)
	char storedPassword[20] = { 0 };

	if (MD5Encryption == 0)
	{
		strncpy(storedPassword, lpMsg->password, sizeof(storedPassword) - 1);
	}
	else
	{
		MD5 MD5Hash;
		if (MD5Encryption == 1)
		{
			MD5Hash.MD5_EncodeString(lpMsg->password, storedPassword, MakeAccountKey(lpMsg->account));
		}
		else
		{
			MD5Hash.MD5_EncodeString(lpMsg->password, storedPassword, 0);
		}
	}

	// Insert new account
#if defined(SQLITE)
	if (gQueryManager.ExecQuery("INSERT INTO MEMB_INFO (memb___id, memb__pwd, memb_name, sno__numb, bloc_code, AccountLevel, AccountExpireDate) VALUES ('%s', '%s', '%s', '1111111111111', 0, 0, datetime('now', '+30 days'))", lpMsg->account, storedPassword, lpMsg->account) == false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("INSERT INTO MEMB_INFO (memb___id, memb__pwd, memb_name, sno__numb, bloc_code, AccountLevel, AccountExpireDate) VALUES ('%s', '%s', '%s', '1111111111111', 0, 0, DATEADD(day, 30, GETDATE()))", lpMsg->account, storedPassword, lpMsg->account) == false)
#else
	if (gQueryManager.ExecUpdateQuery("INSERT INTO MEMB_INFO (memb___id, memb__pwd, memb_name, sno__numb, bloc_code, AccountLevel, AccountExpireDate) VALUES ('%s', '%s', '%s', '1111111111111', 0, 0, DATE_ADD(NOW(), INTERVAL 30 DAY))", lpMsg->account, storedPassword, lpMsg->account) == false)
#endif
	{
		gQueryManager.Close();

		pMsg.result = 3; // Server error

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	gQueryManager.Close();

	LogAdd(LOG_BLUE, "[RegisterAccount] New account registered: '%s' (IP: %s)", lpMsg->account, lpMsg->IpAddress);

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

// Launcher protocol handlers

void GJLauncherLoginRecv(SDHP_LAUNCHER_LOGIN_RECV* lpMsg, int index)
{
	SDHP_LAUNCHER_LOGIN_SEND pMsg;

	pMsg.header.set(0x10, sizeof(pMsg));

	pMsg.result = 1; // Success by default

	if (CheckTextSyntax(lpMsg->account, sizeof(lpMsg->account)) == false)
	{
		pMsg.result = 2; // Account not found / invalid

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Check if account is online
	ACCOUNT_INFO AccountInfo;

	if (gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) != false)
	{
		pMsg.result = 3; // Account is online

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Validate password
	if (MD5Encryption == 0)
	{
	#if defined(SQLITE)
		if (gQueryManager.ExecQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
	#elif !defined(MYSQL)
		if (gQueryManager.ExecQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s' COLLATE Latin1_General_BIN", lpMsg->account) == false || gQueryManager.Fetch() == SQL_NO_DATA)
	#else
		if (gQueryManager.ExecResultQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
	#endif
		{
			gQueryManager.Close();

			pMsg.result = 2; // Account not found

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}

		char password[11] = { 0 };

		gQueryManager.GetAsString("memb__pwd", password, sizeof(password));

		gQueryManager.Close();

		if (strcmp(lpMsg->password, password) != 0)
		{
			pMsg.result = 0; // Invalid password

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}
	}
	else
	{
	#if defined(SQLITE)
		if (gQueryManager.ExecQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
	#elif !defined(MYSQL)
		if (gQueryManager.ExecQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s' COLLATE Latin1_General_BIN", lpMsg->account) == false || gQueryManager.Fetch() == SQL_NO_DATA)
	#else
		if (gQueryManager.ExecResultQuery("SELECT memb__pwd FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) == false || gQueryManager.Fetch() == false)
	#endif
		{
			gQueryManager.Close();

			pMsg.result = 2; // Account not found

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}

		BYTE password[16] = { 0 };

		gQueryManager.GetAsBinary("memb__pwd", password, sizeof(password));

		gQueryManager.Close();

		MD5 MD5Hash;

		if (MD5Encryption == 1)
		{
			if (MD5Hash.MD5_CheckValue(lpMsg->password, (char*)password, MakeAccountKey(lpMsg->account)) == false)
			{
				pMsg.result = 0; // Invalid password

				gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

				return;
			}
		}
		else
		{
			if (MD5Hash.MD5_CheckValue(lpMsg->password, (char*)password) == false)
			{
				pMsg.result = 0; // Invalid password

				gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

				return;
			}
		}
	}

	LogAdd(LOG_BLUE, "[LauncherLogin] Account '%s' logged in from IP: %s", lpMsg->account, lpMsg->IpAddress);

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJLauncherGetCharactersRecv(SDHP_LAUNCHER_GET_CHARACTERS_RECV* lpMsg, int index)
{
	BYTE buffer[2048] = { 0 };

	SDHP_LAUNCHER_GET_CHARACTERS_SEND* pMsg = (SDHP_LAUNCHER_GET_CHARACTERS_SEND*)buffer;

	pMsg->result = 1;
	pMsg->count = 0;

	SDHP_LAUNCHER_CHARACTER_INFO* pCharInfo = (SDHP_LAUNCHER_CHARACTER_INFO*)(buffer + sizeof(SDHP_LAUNCHER_GET_CHARACTERS_SEND));

	// Get character names from AccountCharacter table
	char charNames[5][11] = { 0 };

#if defined(SQLITE)
	if (gQueryManager.ExecQuery("SELECT GameID1, GameID2, GameID3, GameID4, GameID5 FROM AccountCharacter WHERE Id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("SELECT GameID1, GameID2, GameID3, GameID4, GameID5 FROM AccountCharacter WHERE Id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != SQL_NO_DATA)
#else
	if (gQueryManager.ExecResultQuery("SELECT GameID1, GameID2, GameID3, GameID4, GameID5 FROM AccountCharacter WHERE Id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != false)
#endif
	{
		gQueryManager.GetAsString("GameID1", charNames[0], sizeof(charNames[0]));
		gQueryManager.GetAsString("GameID2", charNames[1], sizeof(charNames[1]));
		gQueryManager.GetAsString("GameID3", charNames[2], sizeof(charNames[2]));
		gQueryManager.GetAsString("GameID4", charNames[3], sizeof(charNames[3]));
		gQueryManager.GetAsString("GameID5", charNames[4], sizeof(charNames[4]));
	}

	gQueryManager.Close();

	// Get character info for each character
	for (int i = 0; i < 5; i++)
	{
		if (strlen(charNames[i]) == 0)
		{
			continue;
		}

		// Bounds check to prevent buffer overflow
		WORD currentSize = sizeof(SDHP_LAUNCHER_GET_CHARACTERS_SEND) + (pMsg->count * sizeof(SDHP_LAUNCHER_CHARACTER_INFO));
		if (currentSize + sizeof(SDHP_LAUNCHER_CHARACTER_INFO) > sizeof(buffer))
		{
			break;
		}

	#if defined(SQLITE)
		if (gQueryManager.ExecQuery("SELECT Name, cLevel, ResetCount, GrandResetCount, LevelUpPoint, Strength, Dexterity, Vitality, Energy, PkLevel, Money FROM Character WHERE Name='%s'", charNames[i]) == false || gQueryManager.Fetch() == false)
	#elif !defined(MYSQL)
		if (gQueryManager.ExecQuery("SELECT Name, cLevel, ResetCount, GrandResetCount, LevelUpPoint, Strength, Dexterity, Vitality, Energy, PkLevel, Money FROM Character WHERE Name='%s'", charNames[i]) == false || gQueryManager.Fetch() == SQL_NO_DATA)
	#else
		if (gQueryManager.ExecResultQuery("SELECT Name, cLevel, ResetCount, GrandResetCount, LevelUpPoint, Strength, Dexterity, Vitality, Energy, PkLevel, Money FROM Character WHERE Name='%s'", charNames[i]) == false || gQueryManager.Fetch() == false)
	#endif
		{
			gQueryManager.Close();
			continue;
		}

		gQueryManager.GetAsString("Name", pCharInfo->name, sizeof(pCharInfo->name));
		pCharInfo->level = gQueryManager.GetAsInteger("cLevel");
		pCharInfo->resets = gQueryManager.GetAsInteger("ResetCount");
		pCharInfo->grandResets = gQueryManager.GetAsInteger("GrandResetCount");
		pCharInfo->points = gQueryManager.GetAsInteger("LevelUpPoint");
		pCharInfo->strength = gQueryManager.GetAsInteger("Strength");
		pCharInfo->dexterity = gQueryManager.GetAsInteger("Dexterity");
		pCharInfo->vitality = gQueryManager.GetAsInteger("Vitality");
		pCharInfo->energy = gQueryManager.GetAsInteger("Energy");
		pCharInfo->pkLevel = gQueryManager.GetAsInteger("PkLevel");
		pCharInfo->money = gQueryManager.GetAsInteger("Money");

		gQueryManager.Close();

		pCharInfo++;
		pMsg->count++;
	}

	WORD size = sizeof(SDHP_LAUNCHER_GET_CHARACTERS_SEND) + (pMsg->count * sizeof(SDHP_LAUNCHER_CHARACTER_INFO));

	pMsg->header.set(0x11, size);

	gSocketManager.DataSend(index, buffer, size);
}

void GJLauncherAddPointsRecv(SDHP_LAUNCHER_ADD_POINTS_RECV* lpMsg, int index)
{
	SDHP_LAUNCHER_ADD_POINTS_SEND pMsg;

	pMsg.header.set(0x12, sizeof(pMsg));

	pMsg.result = 1; // Success by default

	// Check if account is online
	ACCOUNT_INFO AccountInfo;

	if (gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) != false)
	{
		pMsg.result = 4; // Account is online

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Validate stat type
	const char* statColumns[] = { "Strength", "Dexterity", "Vitality", "Energy" };

	if (lpMsg->stat > 3)
	{
		pMsg.result = 0; // Failed - invalid stat

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Check available points
#if defined(SQLITE)
	if (gQueryManager.ExecQuery("SELECT LevelUpPoint FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("SELECT LevelUpPoint FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == SQL_NO_DATA)
#else
	if (gQueryManager.ExecResultQuery("SELECT LevelUpPoint FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == false)
#endif
	{
		gQueryManager.Close();

		pMsg.result = 3; // Character not found

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	int availablePoints = gQueryManager.GetAsInteger("LevelUpPoint");

	gQueryManager.Close();

	if (availablePoints < lpMsg->points)
	{
		pMsg.result = 2; // Not enough points

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Update character stats
#if defined(SQLITE)
	gQueryManager.ExecQuery("UPDATE Character SET %s = %s + %d, LevelUpPoint = LevelUpPoint - %d WHERE Name='%s'", statColumns[lpMsg->stat], statColumns[lpMsg->stat], lpMsg->points, lpMsg->points, lpMsg->name);
#elif !defined(MYSQL)
	gQueryManager.ExecQuery("UPDATE Character SET %s = %s + %d, LevelUpPoint = LevelUpPoint - %d WHERE Name='%s'", statColumns[lpMsg->stat], statColumns[lpMsg->stat], lpMsg->points, lpMsg->points, lpMsg->name);
#else
	gQueryManager.ExecUpdateQuery("UPDATE Character SET %s = %s + %d, LevelUpPoint = LevelUpPoint - %d WHERE Name='%s'", statColumns[lpMsg->stat], statColumns[lpMsg->stat], lpMsg->points, lpMsg->points, lpMsg->name);
#endif

	gQueryManager.Close();

	LogAdd(LOG_BLUE, "[LauncherAddPoints] Character '%s' added %d points to %s", lpMsg->name, lpMsg->points, statColumns[lpMsg->stat]);

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJLauncherClearPKRecv(SDHP_LAUNCHER_CLEAR_PK_RECV* lpMsg, int index)
{
	if (ClearPK == 0)
	{
		return;
	}
	SDHP_LAUNCHER_CLEAR_PK_SEND pMsg;

	pMsg.header.set(0x13, sizeof(pMsg));

	pMsg.result = 1; // Success by default

	// Check if account is online
	ACCOUNT_INFO AccountInfo;

	if (gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) != false)
	{
		pMsg.result = 4; // Account is online

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Check character money
#if defined(SQLITE)
	if (gQueryManager.ExecQuery("SELECT Money FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("SELECT Money FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == SQL_NO_DATA)
#else
	if (gQueryManager.ExecResultQuery("SELECT Money FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == false)
#endif
	{
		gQueryManager.Close();

		pMsg.result = 3; // Character not found

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	int money = gQueryManager.GetAsInteger("Money");

	gQueryManager.Close();

	if (money < lpMsg->zenCost)
	{
		pMsg.result = 2; // Not enough zen

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Clear PK status
#if defined(SQLITE)
	gQueryManager.ExecQuery("UPDATE Character SET PkLevel = 3, PkCount = 0, PkTime = 0, Money = Money - %d WHERE Name='%s'", lpMsg->zenCost, lpMsg->name);
#elif !defined(MYSQL)
	gQueryManager.ExecQuery("UPDATE Character SET PkLevel = 3, PkCount = 0, PkTime = 0, Money = Money - %d WHERE Name='%s'", lpMsg->zenCost, lpMsg->name);
#else
	gQueryManager.ExecUpdateQuery("UPDATE Character SET PkLevel = 3, PkCount = 0, PkTime = 0, Money = Money - %d WHERE Name='%s'", lpMsg->zenCost, lpMsg->name);
#endif

	gQueryManager.Close();

	LogAdd(LOG_BLUE, "[LauncherClearPK] Character '%s' cleared PK status for %d zen", lpMsg->name, lpMsg->zenCost);

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJLauncherResetRecv(SDHP_LAUNCHER_RESET_RECV* lpMsg, int index)
{
    JS_LoadCommandInfo(); // ensure config loaded

    SDHP_LAUNCHER_RESET_SEND pMsg;

    pMsg.header.set(0x14, sizeof(pMsg));

    pMsg.result = 1; // Success by default

    // Determine account level from database to select ALx configuration
    int accountLevel = 0;
#if defined(SQLITE)
    if (gQueryManager.ExecQuery("SELECT AccountLevel FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != false)
    {
        accountLevel = gQueryManager.GetAsInteger("AccountLevel");
    }
    gQueryManager.Close();
#elif !defined(MYSQL)
    if (gQueryManager.ExecQuery("EXEC WZ_GetAccountLevel '%s'", lpMsg->account) != false && gQueryManager.Fetch() != SQL_NO_DATA)
    {
        accountLevel = gQueryManager.GetAsInteger("AccountLevel");
        gQueryManager.Close();
    }
    else
    {
        gQueryManager.Close();
    }
#else
    if (gQueryManager.ExecResultQuery("CALL WZ_GetAccountLevel('%s')", lpMsg->account) != false && gQueryManager.Fetch() != false)
    {
        accountLevel = gQueryManager.GetAsInteger("AccountLevel");
    }
    gQueryManager.Close();
#endif

    if (accountLevel < 0 || accountLevel > 3) accountLevel = 0;

    // Use server-side configured required level based on account level
    WORD requiredLevel = (WORD)g_JS_CommandResetLevel[accountLevel];

    // Check if account is online
    ACCOUNT_INFO AccountInfo;

    if (gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) != false)
    {
        pMsg.result = 4; // Account is online

        gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

        return;
    }

    // Check character level
#if defined(SQLITE)
    if (gQueryManager.ExecQuery("SELECT cLevel FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == false)
#elif !defined(MYSQL)
    if (gQueryManager.ExecQuery("SELECT cLevel FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == SQL_NO_DATA)
#else
    if (gQueryManager.ExecResultQuery("SELECT cLevel FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == false)
#endif
    {
        gQueryManager.Close();

        pMsg.result = 3; // Character not found

        gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

        return;
    }

    int level = gQueryManager.GetAsInteger("cLevel");

    gQueryManager.Close();

    if (level < requiredLevel)
    {
        pMsg.result = 2; // Level too low

        gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

        return;
    }

    // Perform reset
#if defined(SQLITE)
    gQueryManager.ExecQuery("UPDATE Character SET cLevel = 1, Experience = 0, ResetCount = ResetCount + 1, LevelUpPoint = LevelUpPoint + %d, Strength = 25, Dexterity = 25, Vitality = 25, Energy = 25, MapNumber = 0, MapPosX = 125, MapPosY = 125 WHERE Name='%s'", lpMsg->rewardPoints, lpMsg->name);
#elif !defined(MYSQL)
    gQueryManager.ExecQuery("UPDATE Character SET cLevel = 1, Experience = 0, ResetCount = ResetCount + 1, LevelUpPoint = LevelUpPoint + %d, Strength = 25, Dexterity = 25, Vitality = 25, Energy = 25, MapNumber = 0, MapPosX = 125, MapPosY = 125 WHERE Name='%s'", lpMsg->rewardPoints, lpMsg->name);
#else
    gQueryManager.ExecUpdateQuery("UPDATE Character SET cLevel = 1, Experience = 0, ResetCount = ResetCount + 1, LevelUpPoint = LevelUpPoint + %d, Strength = 25, Dexterity = 25, Vitality = 25, Energy = 25, MapNumber = 0, MapPosX = 125, MapPosY = 125 WHERE Name='%s'", lpMsg->rewardPoints, lpMsg->name);
#endif

    gQueryManager.Close();

    LogAdd(LOG_BLUE, "[LauncherReset] Character '%s' performed reset", lpMsg->name);

    gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJLauncherGrandResetRecv(SDHP_LAUNCHER_GRAND_RESET_RECV* lpMsg, int index)
{
    JS_LoadCommandInfo(); // ensure config loaded

    SDHP_LAUNCHER_GRAND_RESET_SEND pMsg;

    pMsg.header.set(0x15, sizeof(pMsg));

    pMsg.result = 1; // Success by default

    // Check if account is online
    ACCOUNT_INFO AccountInfo;

    if (gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) != false)
    {
        pMsg.result = 5; // Account is online

        gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

        return;
    }

    // Determine account level from database to select ALx configuration
    int accountLevel = 0;
#if defined(SQLITE)
    if (gQueryManager.ExecQuery("SELECT AccountLevel FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != false)
    {
        accountLevel = gQueryManager.GetAsInteger("AccountLevel");
    }
    gQueryManager.Close();
#elif !defined(MYSQL)
    if (gQueryManager.ExecQuery("EXEC WZ_GetAccountLevel '%s'", lpMsg->account) != false && gQueryManager.Fetch() != SQL_NO_DATA)
    {
        accountLevel = gQueryManager.GetAsInteger("AccountLevel");
        gQueryManager.Close();
    }
    else
    {
        gQueryManager.Close();
    }
#else
    if (gQueryManager.ExecResultQuery("CALL WZ_GetAccountLevel('%s')", lpMsg->account) != false && gQueryManager.Fetch() != false)
    {
        accountLevel = gQueryManager.GetAsInteger("AccountLevel");
    }
    gQueryManager.Close();
#endif

    if (accountLevel < 0 || accountLevel > 3) accountLevel = 0;

    // Check character level and resets
#if defined(SQLITE)
    if (gQueryManager.ExecQuery("SELECT cLevel, ResetCount FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == false)
#elif !defined(MYSQL)
    if (gQueryManager.ExecQuery("SELECT cLevel, ResetCount FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == SQL_NO_DATA)
#else
    if (gQueryManager.ExecResultQuery("SELECT cLevel, ResetCount FROM Character WHERE Name='%s'", lpMsg->name) == false || gQueryManager.Fetch() == false)
#endif
    {
        gQueryManager.Close();

        pMsg.result = 4; // Character not found

        gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

        return;
    }

    int level = gQueryManager.GetAsInteger("cLevel");
    int resets = gQueryManager.GetAsInteger("ResetCount");

    gQueryManager.Close();

    // Use server-side configured required level and resets based on account level
    WORD requiredLevel = (WORD)g_JS_CommandGrandResetLevel[accountLevel];
    WORD requiredResets = (WORD)g_JS_CommandGrandResetReset[accountLevel];

    if (resets < requiredResets)
    {
        pMsg.result = 3; // Resets too low

        gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

        return;
    }

    if (level < requiredLevel)
    {
        pMsg.result = 2; // Level too low

        gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

        return;
    }

    // Perform grand reset
#if defined(SQLITE)
	gQueryManager.ExecQuery("UPDATE Character SET cLevel = 1, Experience = 0, ResetCount = 0, GrandResetCount = GrandResetCount + 1, LevelUpPoint = LevelUpPoint + %d, Strength = 25, Dexterity = 25, Vitality = 25, Energy = 25, MapNumber = 0, MapPosX = 125, MapPosY = 125 WHERE Name='%s'", lpMsg->rewardPoints, lpMsg->name);
#elif !defined(MYSQL)
	gQueryManager.ExecQuery("UPDATE Character SET cLevel = 1, Experience = 0, ResetCount = 0, GrandResetCount = GrandResetCount + 1, LevelUpPoint = LevelUpPoint + %d, Strength = 25, Dexterity = 25, Vitality = 25, Energy = 25, MapNumber = 0, MapPosX = 125, MapPosY = 125 WHERE Name='%s'", lpMsg->rewardPoints, lpMsg->name);
#else
	gQueryManager.ExecUpdateQuery("UPDATE Character SET cLevel = 1, Experience = 0, ResetCount = 0, GrandResetCount = GrandResetCount + 1, LevelUpPoint = LevelUpPoint + %d, Strength = 25, Dexterity = 25, Vitality = 25, Energy = 25, MapNumber = 0, MapPosX = 125, MapPosY = 125 WHERE Name='%s'", lpMsg->rewardPoints, lpMsg->name);
#endif

	gQueryManager.Close();

	LogAdd(LOG_BLUE, "[LauncherGrandReset] Character '%s' performed grand reset", lpMsg->name);

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJLauncherCheckOnlineRecv(SDHP_LAUNCHER_CHECK_ONLINE_RECV* lpMsg, int index)
{
	SDHP_LAUNCHER_CHECK_ONLINE_SEND pMsg;

	pMsg.header.set(0x16, sizeof(pMsg));

	ACCOUNT_INFO AccountInfo;

	pMsg.result = gAccountManager.GetAccountInfo(&AccountInfo, lpMsg->account) != false ? 1 : 0;

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}

void GJLauncherRegisterRecv(SDHP_LAUNCHER_REGISTER_RECV* lpMsg, int index)
{
	SDHP_LAUNCHER_REGISTER_SEND pMsg;

	pMsg.header.set(0x17, sizeof(pMsg));

	pMsg.result = 1; // Success by default

	// Validate account name (4-10 chars, alphanumeric and underscore only)
	int accountLen = (int)strlen(lpMsg->account);
	if (accountLen < 4 || accountLen > 10)
	{
		pMsg.result = 2; // Invalid input

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	for (int i = 0; i < accountLen; i++)
	{
		char c = lpMsg->account[i];
		if (!isalnum(c) && c != '_')
		{
			pMsg.result = 2; // Invalid input

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}
	}

	// Validate password (4-10 chars)
	int passLen = (int)strlen(lpMsg->password);
	if (passLen < 4 || passLen > 10)
	{
		pMsg.result = 2; // Invalid input

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Validate password for SQL injection (no quotes or spaces)
	if (CheckTextSyntax(lpMsg->password, sizeof(lpMsg->password)) == false)
	{
		pMsg.result = 2; // Invalid input

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	// Validate personal code (4-10 chars, alphanumeric and underscore only)
	int codeLen = (int)strlen(lpMsg->personalCode);
	if (codeLen < 4 || codeLen > 10)
	{
		pMsg.result = 2; // Invalid input

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	for (int i = 0; i < codeLen; i++)
	{
		char c = lpMsg->personalCode[i];
		if (!isalnum(c) && c != '_')
		{
			pMsg.result = 2; // Invalid input

			gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

			return;
		}
	}

	// Check if account already exists
#if defined(SQLITE)
	if (gQueryManager.ExecQuery("SELECT memb___id FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("SELECT memb___id FROM MEMB_INFO WHERE memb___id='%s' COLLATE Latin1_General_BIN", lpMsg->account) != false && gQueryManager.Fetch() != SQL_NO_DATA)
#else
	if (gQueryManager.ExecResultQuery("SELECT memb___id FROM MEMB_INFO WHERE memb___id='%s'", lpMsg->account) != false && gQueryManager.Fetch() != false)
#endif
	{
		gQueryManager.Close();

		pMsg.result = 0; // Account already exists

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	gQueryManager.Close();

	// Prepare password (encode with MD5 if enabled)
	char storedPassword[20] = { 0 };

	if (MD5Encryption == 0)
	{
		strncpy(storedPassword, lpMsg->password, sizeof(storedPassword) - 1);
	}
	else
	{
		MD5 MD5Hash;
		if (MD5Encryption == 1)
		{
			MD5Hash.MD5_EncodeString(lpMsg->password, storedPassword, MakeAccountKey(lpMsg->account));
		}
		else
		{
			MD5Hash.MD5_EncodeString(lpMsg->password, storedPassword, 0);
		}
	}

	// Insert new account
#if defined(SQLITE)
	if (gQueryManager.ExecQuery("INSERT INTO MEMB_INFO (memb___id, memb__pwd, memb_name, sno__numb, bloc_code, AccountLevel, AccountExpireDate) VALUES ('%s', '%s', '%s', '1111111111111', 0, 0, datetime('now', '+30 days'))", lpMsg->account, storedPassword, lpMsg->personalCode) == false)
#elif !defined(MYSQL)
	if (gQueryManager.ExecQuery("INSERT INTO MEMB_INFO (memb___id, memb__pwd, memb_name, sno__numb, bloc_code, AccountLevel, AccountExpireDate) VALUES ('%s', '%s', '%s', '1111111111111', 0, 0, DATEADD(day, 30, GETDATE()))", lpMsg->account, storedPassword, lpMsg->personalCode) == false)
#else
	if (gQueryManager.ExecUpdateQuery("INSERT INTO MEMB_INFO (memb___id, memb__pwd, memb_name, sno__numb, bloc_code, AccountLevel, AccountExpireDate) VALUES ('%s', '%s', '%s', '1111111111111', 0, 0, DATE_ADD(NOW(), INTERVAL 30 DAY))", lpMsg->account, storedPassword, lpMsg->personalCode) == false)
#endif
	{
		gQueryManager.Close();

		pMsg.result = 3; // Server error

		gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);

		return;
	}

	gQueryManager.Close();

	LogAdd(LOG_BLUE, "[LauncherRegister] New account registered: '%s' (Name: %s, IP: %s)", lpMsg->account, lpMsg->personalCode, lpMsg->IpAddress);

	gSocketManager.DataSend(index, (BYTE*)&pMsg, pMsg.header.size);
}