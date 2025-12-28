#pragma once

#include "ProtocolDefines.h"
#include "User.h"

enum eCommandNumber
{
	COMMAND_MOVE = 0,				// 移动
	COMMAND_POST = 1,				// 邮件
	COMMAND_ADD_STR = 2,			// 增加力量
	COMMAND_ADD_DEX = 3,			// 增加敏捷
	COMMAND_ADD_VIT = 4,			// 增加体力
	COMMAND_ADD_ENE = 5,			// 增加能量
	COMMAND_RESET = 6,				// 重置（普通重置）
	COMMAND_GRAND_RESET = 7,		// 大师重置（转世）
	COMMAND_PK_CLEAR = 8,			// 清除PK值
	COMMAND_ADD_MONEY = 9,			// 增加金钱
	COMMAND_SUB_MONEY = 10,			// 减少金钱
	COMMAND_CHANGE = 11,			// 职业变更
	COMMAND_WARE = 12,				// 仓库
	COMMAND_ONLINES = 13,			// 在线人数
	COMMAND_GUILD_WAR = 14,			// 公会战争
	COMMAND_BATTLE_SOCCER = 15,		// 战场足球
	COMMAND_REQUEST = 16,			// 请求/申请
	COMMAND_GM_GLOBAL = 17,			// GM全局公告
	COMMAND_GM_MOVE = 18,			// GM移动
	COMMAND_GM_MOVEALL = 19,		// GM移动所有人
	COMMAND_GM_CHASE = 20,			// GM追踪玩家
	COMMAND_GM_BRING = 21,			// GM召唤玩家
	COMMAND_GM_DISCONNECT = 22,		// GM断开连接
	COMMAND_GM_FIREWORKS = 23,		// GM烟花效果
	COMMAND_GM_DROP = 24,			// GM掉落物品
	COMMAND_GM_MAKE = 25,			// GM制造物品
	COMMAND_GM_MAKESET = 26,		// GM制造套装
	COMMAND_GM_CLEARINV = 27,		// GM清理背包
	COMMAND_GM_SKIN = 28,			// GM皮肤/外观
	COMMAND_GM_MAKEMOB = 29,		// GM生成怪物
	COMMAND_Clear_Stats = 30,		//洗点
};

//**********************************************//
//********** DataServer -> GameServer **********//
//**********************************************//

struct SDHP_GLOBAL_POST_RECV
{
	PSBMSG_HEAD header; // C1:05:03
	BYTE type;
	char name[11];
	char message[60];
	char serverName[60];
};

struct SDHP_COMMAND_RESET_RECV
{
	PSBMSG_HEAD header; // C1:05:04 | C1:05:05  转生 | 转世
	WORD index;
	char account[11];
	char name[11];
	UINT ResetDay;
	UINT ResetWek;
	UINT ResetMon;
};

//**********************************************//
//********** GameServer -> DataServer **********//
//**********************************************//

struct SDHP_GLOBAL_POST_SEND
{
	PSBMSG_HEAD header; // C1:05:03
	BYTE type;
	char name[11];
	char message[60];
	char serverName[60];
};

struct SDHP_COMMAND_RESET_SEND
{
	PSBMSG_HEAD header; // C1:05:04 | C1:05:05
	WORD index;
	char account[11];
	char name[11];
};

//**********************************************//
//********** JoinServer -> GameServer **********//
//**********************************************//

struct SDHP_COUNT_ONLINE_USER_RECV
{
	PBMSG_HEAD header; // C1:07
	int index;
	int count;
};

//**********************************************//
//********** GameServer -> JoinServer **********//
//**********************************************//

struct SDHP_COUNT_ONLINE_USER_SEND
{
	PBMSG_HEAD header; // C1:07
	int index;
};

//**********************************************//
//**********************************************//
//**********************************************//

struct COMMAND_LIST
{
	int Index;
	char Command[128];
	int Enable[MAX_ACCOUNT_LEVEL];
	int Money[MAX_ACCOUNT_LEVEL];
	int MinLevel[MAX_ACCOUNT_LEVEL];
	int MaxLevel[MAX_ACCOUNT_LEVEL];
	int MinReset[MAX_ACCOUNT_LEVEL];
	int MaxReset[MAX_ACCOUNT_LEVEL];
	int Delay;
	int GameMaster;
};

class CCommandManager
{
public:

	CCommandManager();

	~CCommandManager();

	void Load(char* path);

	void MainProc();

	long GetNumber(char* arg, int pos);

	void GetString(char* arg, char* out, int size, int pos);

	bool GetInfoByName(char* label, COMMAND_LIST* lpInfo);

	bool ManagementCore(LPOBJ lpObj, char* message);

	//Commands

	void CommandMove(LPOBJ lpObj, char* arg);

	void CommandPost(LPOBJ lpObj, char* arg);

	void GDGlobalPostSend(BYTE type, char* name, char* message);

	void DGGlobalPostRecv(SDHP_GLOBAL_POST_RECV* lpMsg);

	void GCPostMessageGold(char* name, char* serverName, int message, char* text);

	void GCPostMessageBlue(char* name, char* serverName, int message, char* text);

	void GCPostMessageGreen(char* name, char* serverName, int message, char* text);

	void CommandAddPoint(LPOBJ lpObj, char* arg, int type);

	void CommandAddPointAuto(LPOBJ lpObj, char* arg, int type);

	void CommandAddPointAutoProc(LPOBJ lpObj);

	void CommandReset(LPOBJ lpObj, char* arg);

	void CommandResetAuto(LPOBJ lpObj, char* arg);

	void CommandResetAutoProc(LPOBJ lpObj);

	void DGCommandResetRecv(SDHP_COMMAND_RESET_RECV* lpMsg);

	void CommandGrandReset(LPOBJ lpObj, char* arg);

	void DGCommandGrandResetRecv(SDHP_COMMAND_RESET_RECV* lpMsg);

	void CommandPKClear(LPOBJ lpObj, char* arg);

	void CommandAddMoney(LPOBJ lpObj, char* arg);

	void CommandSubMoney(LPOBJ lpObj, char* arg);

	void CommandChange(LPOBJ lpObj, char* arg);

	void CommandWare(LPOBJ lpObj, char* arg);

	void CommandOnlines(LPOBJ lpObj, char* arg);

	void JGCommandOnlinesRecv(SDHP_COUNT_ONLINE_USER_RECV* lpMsg);

	void CommandGuildWar(LPOBJ lpObj, char* arg);

	void CommandBattleSoccer(LPOBJ lpObj, char* arg);

	void CommandRequest(LPOBJ lpObj, char* arg);

	void CommandGMGlobal(LPOBJ lpObj, char* arg);

	void CommandGMMove(LPOBJ lpObj, char* arg);

	void CommandGMMoveAll(LPOBJ lpObj, char* arg);

	void CommandGMChase(LPOBJ lpObj, char* arg);

	void CommandGMBring(LPOBJ lpObj, char* arg);

	void CommandGMDisconnect(LPOBJ lpObj, char* arg);

	void CommandGMFireworks(LPOBJ lpObj, char* arg);

	void CommandGMDrop(LPOBJ lpObj, char* arg);

	void CommandGMMake(LPOBJ lpObj, char* arg);

	void CommandGMMakeSet(LPOBJ lpObj, char* arg);

	void CommandGMClearInv(LPOBJ lpObj, char* arg);

	void CommandGMSkin(LPOBJ lpObj, char* arg);

	void CommandGMMakeMob(LPOBJ lpObj, char* arg);

	void CommandClearStats(LPOBJ lpObj, char* arg);

private:

	std::map<int, COMMAND_LIST> m_CommandInfo;
};

extern CCommandManager gCommandManager;