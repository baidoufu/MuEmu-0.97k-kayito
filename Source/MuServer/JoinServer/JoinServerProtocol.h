#pragma once

#include "ProtocolDefines.h"

//**********************************************//
//********** JoinServer -> ConnectServer *******//
//**********************************************//

struct SDHP_JOIN_SERVER_LIVE_SEND
{
	PBMSG_HEAD header; // C1:02
	DWORD QueueSize;
};

//**********************************************//
//********** GameServer -> JoinServer **********//
//**********************************************//

struct SDHP_SERVER_INFO_RECV
{
	PBMSG_HEAD header; // C1:00
	BYTE type;
	WORD ServerPort;
	char ServerName[50];
	WORD ServerCode;
};

struct SDHP_CONNECT_ACCOUNT_RECV
{
	PBMSG_HEAD header; // C1:01
	WORD index;
	char account[11];
	char password[11];
	char IpAddress[16];
};

struct SDHP_DISCONNECT_ACCOUNT_RECV
{
	PBMSG_HEAD header; // C1:02
	WORD index;
	char account[11];
	char IpAddress[16];
};

struct SDHP_ACCOUNT_LEVEL_RECV
{
	PBMSG_HEAD header; // C1:03
	WORD index;
	char account[11];
};

struct SDHP_ACCOUNT_LEVEL_SAVE_RECV
{
	PBMSG_HEAD header; // C1:04
	WORD index;
	char account[11];
	WORD AccountLevel;
	DWORD AccountExpireTime;
};

struct SDHP_SERVER_USER_INFO_RECV
{
	PBMSG_HEAD header; // C1:06
	WORD CurUserCount;
	WORD MaxUserCount;
};

struct SDHP_COUNT_ONLINE_USER_RECV
{
	PBMSG_HEAD header; // C1:07
	int index;
};

struct SDHP_REGISTER_ACCOUNT_RECV
{
	PBMSG_HEAD header; // C1:08
	WORD index;
	char account[11];
	char password[11];
	char IpAddress[16];
};

//**********************************************//
//********** JoinServer -> GameServer **********//
//**********************************************//

struct SDHP_CONNECT_ACCOUNT_SEND
{
	PBMSG_HEAD header; // C1:01
	WORD index;
	char account[11];
	char PersonalCode[14];
	BYTE result;
	BYTE BlockCode;
	WORD AccountLevel;
	char AccountExpireDate[20];
};

struct SDHP_DISCONNECT_ACCOUNT_SEND
{
	PBMSG_HEAD header; // C1:02
	WORD index;
	char account[11];
	BYTE result;
};

struct SDHP_ACCOUNT_LEVEL_SEND
{
	PBMSG_HEAD header; // C1:03
	WORD index;
	char account[11];
	WORD AccountLevel;
	char AccountExpireDate[20];
};

struct SDHP_ACCOUNT_ALREADY_CONNECTED_SEND
{
	PBMSG_HEAD header; // C1:05
	WORD index;
	char account[11];
};

struct SDHP_COUNT_ONLINE_USER_SEND
{
	PBMSG_HEAD header; // C1:07
	int index;
	int count;
};

struct SDHP_REGISTER_ACCOUNT_SEND
{
	PBMSG_HEAD header; // C1:08
	WORD index;
	BYTE result;
};

//**********************************************//
//********** Launcher -> JoinServer ************//
//**********************************************//

struct SDHP_LAUNCHER_LOGIN_RECV
{
	PBMSG_HEAD header; // C1:10
	char account[11];
	char password[11];
	char IpAddress[16];
};

struct SDHP_LAUNCHER_GET_CHARACTERS_RECV
{
	PBMSG_HEAD header; // C1:11
	char account[11];
};

struct SDHP_LAUNCHER_ADD_POINTS_RECV
{
	PBMSG_HEAD header; // C1:12
	char account[11];
	char name[11];
	BYTE stat; // 0=Strength, 1=Dexterity, 2=Vitality, 3=Energy
	WORD points;
};

struct SDHP_LAUNCHER_CLEAR_PK_RECV
{
	PBMSG_HEAD header; // C1:13
	char account[11];
	char name[11];
	int zenCost;
};

struct SDHP_LAUNCHER_RESET_RECV
{
	PBMSG_HEAD header; // C1:14
	char account[11];
	char name[11];
	WORD requiredLevel;
	int rewardPoints;
};

struct SDHP_LAUNCHER_GRAND_RESET_RECV
{
	PBMSG_HEAD header; // C1:15
	char account[11];
	char name[11];
	WORD requiredLevel;
	WORD requiredResets;
	int rewardPoints;
};

struct SDHP_LAUNCHER_CHECK_ONLINE_RECV
{
	PBMSG_HEAD header; // C1:16
	char account[11];
};

//**********************************************//
//********** JoinServer -> Launcher ************//
//**********************************************//

struct SDHP_LAUNCHER_LOGIN_SEND
{
	PBMSG_HEAD header; // C1:10
	BYTE result; // 0=Invalid password, 1=Success, 2=Account not found, 3=Account online
};

struct SDHP_LAUNCHER_CHARACTER_INFO
{
	char name[11];
	int level;
	int resets;
	int grandResets;
	int points;
	int strength;
	int dexterity;
	int vitality;
	int energy;
	int pkLevel;
	int money;
};

struct SDHP_LAUNCHER_GET_CHARACTERS_SEND
{
	PWMSG_HEAD header; // C2:11
	BYTE result; // 0=Failed, 1=Success
	BYTE count;
	// Followed by SDHP_LAUNCHER_CHARACTER_INFO[count]
};

struct SDHP_LAUNCHER_ADD_POINTS_SEND
{
	PBMSG_HEAD header; // C1:12
	BYTE result; // 0=Failed, 1=Success, 2=Not enough points, 3=Character not found, 4=Account online
};

struct SDHP_LAUNCHER_CLEAR_PK_SEND
{
	PBMSG_HEAD header; // C1:13
	BYTE result; // 0=Failed, 1=Success, 2=Not enough zen, 3=Character not found, 4=Account online
};

struct SDHP_LAUNCHER_RESET_SEND
{
	PBMSG_HEAD header; // C1:14
	BYTE result; // 0=Failed, 1=Success, 2=Level too low, 3=Character not found, 4=Account online
};

struct SDHP_LAUNCHER_GRAND_RESET_SEND
{
	PBMSG_HEAD header; // C1:15
	BYTE result; // 0=Failed, 1=Success, 2=Level too low, 3=Resets too low, 4=Character not found, 5=Account online
};

struct SDHP_LAUNCHER_CHECK_ONLINE_SEND
{
	PBMSG_HEAD header; // C1:16
	BYTE result; // 0=Offline, 1=Online
};

//**********************************************//
//**********************************************//
//**********************************************//

void JoinServerProtocolCore(int index, BYTE head, BYTE* lpMsg, int size);

void JoinServerLiveProc();

void GJServerInfoRecv(SDHP_SERVER_INFO_RECV* lpMsg, int index);

void GJConnectAccountRecv(SDHP_CONNECT_ACCOUNT_RECV* lpMsg, int index);

void GJDisconnectAccountRecv(SDHP_DISCONNECT_ACCOUNT_RECV* lpMsg, int index);

void GJAccountLevelRecv(SDHP_ACCOUNT_LEVEL_RECV* lpMsg, int index);

void GJAccountLevelSaveRecv(SDHP_ACCOUNT_LEVEL_SAVE_RECV* lpMsg, int index);

void JGAccountAlreadyConnectedSend(int GameServerCode, int UserIndex, char* account);

void GJAccountCountRecv(SDHP_COUNT_ONLINE_USER_RECV* lpMsg, int index);

void GJServerUserInfoRecv(SDHP_SERVER_USER_INFO_RECV* lpMsg, int index);

void GJRegisterAccountRecv(SDHP_REGISTER_ACCOUNT_RECV* lpMsg, int index);

// Launcher protocol handlers
void GJLauncherLoginRecv(SDHP_LAUNCHER_LOGIN_RECV* lpMsg, int index);

void GJLauncherGetCharactersRecv(SDHP_LAUNCHER_GET_CHARACTERS_RECV* lpMsg, int index);

void GJLauncherAddPointsRecv(SDHP_LAUNCHER_ADD_POINTS_RECV* lpMsg, int index);

void GJLauncherClearPKRecv(SDHP_LAUNCHER_CLEAR_PK_RECV* lpMsg, int index);

void GJLauncherResetRecv(SDHP_LAUNCHER_RESET_RECV* lpMsg, int index);

void GJLauncherGrandResetRecv(SDHP_LAUNCHER_GRAND_RESET_RECV* lpMsg, int index);

void GJLauncherCheckOnlineRecv(SDHP_LAUNCHER_CHECK_ONLINE_RECV* lpMsg, int index);