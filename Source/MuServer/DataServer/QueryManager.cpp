#include "stdafx.h"
#include "QueryManager.h"
#include "Util.h"

#if defined(SQLITE)

CQueryManager gQueryManager;

CQueryManager::CQueryManager()
{
	this->m_db = NULL;

	this->m_stmt = NULL;

	memset(this->m_dbPath, 0, sizeof(this->m_dbPath));

	this->m_RowCount = -1;

	this->m_ColCount = -1;

	memset(this->m_SQLColName, 0, sizeof(this->m_SQLColName));

	memset(this->m_SQLData, 0, sizeof(this->m_SQLData));

	memset(this->m_BindParams, 0, sizeof(this->m_BindParams));
}

CQueryManager::~CQueryManager()
{
	this->Disconnect();
}

bool CQueryManager::Connect(char* dbPath)
{
	strcpy_s(this->m_dbPath, dbPath);

	int result = sqlite3_open(this->m_dbPath, &this->m_db);

	if (result != SQLITE_OK)
	{
		LogAdd(LOG_RED, "[QueryManager] SQLite 打开数据库失败: %s", sqlite3_errmsg(this->m_db));
		return false;
	}

	// Set busy timeout to 5000ms (5 seconds) to handle concurrent access
	sqlite3_busy_timeout(this->m_db, 5000);

	// Enable WAL mode for better concurrent read/write access
	char* errMsg = NULL;
	result = sqlite3_exec(this->m_db, "PRAGMA journal_mode=WAL;", NULL, NULL, &errMsg);
	if (result != SQLITE_OK)
	{
		LogAdd(LOG_RED, "[QueryManager] SQLite 设置WAL模式失败: %s", errMsg ? errMsg : "未知错误");
		if (errMsg) sqlite3_free(errMsg);
	}

	LogAdd(LOG_BLUE, "[QueryManager] SQLite 数据库连接成功: %s", this->m_dbPath);

	return true;
}

void CQueryManager::Disconnect()
{
	this->Close();

	if (this->m_db != NULL)
	{
		// Try to checkpoint and truncate WAL so data is merged into the main DB file
		char* errMsg = NULL;
		int rc = sqlite3_exec(this->m_db, "PRAGMA wal_checkpoint(TRUNCATE);", NULL, NULL, &errMsg);
		if (rc != SQLITE_OK)
		{
			LogAdd(LOG_RED, "[QueryManager] SQLite WAL checkpoint failed: %s", errMsg ? errMsg : "Unknown error");
			if (errMsg) sqlite3_free(errMsg);
		}
		else
		{
			if (errMsg) sqlite3_free(errMsg);
			LogAdd(LOG_BLUE, "[QueryManager] SQLite WAL checkpoint succeeded: %s", this->m_dbPath);
		}

		sqlite3_close(this->m_db);

		this->m_db = NULL;
	}
}

void CQueryManager::Diagnostic(const char* query)
{
	LogAdd(LOG_BLACK, "%s", query);

	if (this->m_db != NULL)
	{
		LogAdd(LOG_RED, "[QueryManager] 状态 (%d), 诊断信息: %s", sqlite3_errcode(this->m_db), sqlite3_errmsg(this->m_db));
	}
}

bool CQueryManager::ExecQuery(char* query, ...)
{
	char buff[4096];

	va_list arg;

	va_start(arg, query);

	vsprintf_s(buff, query, arg);

	va_end(arg);

	this->Close();

	int result = sqlite3_prepare_v2(this->m_db, buff, -1, &this->m_stmt, NULL);

	if (result != SQLITE_OK)
	{
		this->Diagnostic(buff);

		this->ClearBindings();

		return false;
	}

	// Apply any pending bindings after statement is prepared
	this->ApplyBindings();

	this->m_ColCount = sqlite3_column_count(this->m_stmt);

	if (this->m_ColCount == 0)
	{
		// This is an UPDATE/INSERT/DELETE statement, execute it
		result = sqlite3_step(this->m_stmt);

		if (result != SQLITE_DONE && result != SQLITE_ROW)
		{
			this->Diagnostic(buff);

			return false;
		}

		this->m_RowCount = sqlite3_changes(this->m_db);

		return true;
	}

	if (this->m_ColCount > MAX_COLUMNS)
	{
		return false;
	}

	memset(this->m_SQLColName, 0, sizeof(this->m_SQLColName));

	memset(this->m_SQLData, 0, sizeof(this->m_SQLData));

	for (int n = 0; n < this->m_ColCount; n++)
	{
		const char* colName = sqlite3_column_name(this->m_stmt, n);

		if (colName != NULL)
		{
			strncpy_s(this->m_SQLColName[n], colName, sizeof(this->m_SQLColName[n]) - 1);
		}
	}

	return true;
}

void CQueryManager::Close()
{
	if (this->m_stmt != NULL)
	{
		sqlite3_finalize(this->m_stmt);

		this->m_stmt = NULL;
	}
}

bool CQueryManager::Fetch()
{
	if (this->m_stmt == NULL)
	{
		return false;
	}

	int result = sqlite3_step(this->m_stmt);

	if (result == SQLITE_ROW)
	{
		memset(this->m_SQLData, 0, sizeof(this->m_SQLData));

		for (int n = 0; n < this->m_ColCount; n++)
		{
			const unsigned char* text = sqlite3_column_text(this->m_stmt, n);

			if (text != NULL)
			{
				strncpy_s(this->m_SQLData[n], (const char*)text, sizeof(this->m_SQLData[n]) - 1);
			}

			this->m_SQLDataLen[n] = sqlite3_column_bytes(this->m_stmt, n);
		}

		return true;
	}

	return false;
}

int CQueryManager::FindIndex(char* ColName)
{
	for (int n = 0; n < this->m_ColCount; n++)
	{
		if (_stricmp(ColName, this->m_SQLColName[n]) == 0)
		{
			return n;
		}
	}

	return -1;
}

int CQueryManager::GetResult(int index)
{
	return atoi(this->m_SQLData[index]);
}

int CQueryManager::GetAsInteger(char* ColName)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		return index;
	}
	else
	{
		return atoi(this->m_SQLData[index]);
	}
}

float CQueryManager::GetAsFloat(char* ColName)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		return (float)index;
	}
	else
	{
		return (float)atof(this->m_SQLData[index]);
	}
}

__int64 CQueryManager::GetAsInteger64(char* ColName)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		return index;
	}
	else
	{
		return _atoi64(this->m_SQLData[index]);
	}
}

void CQueryManager::GetAsString(char* ColName, char* OutBuffer, int OutBufferSize)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		memset(OutBuffer, 0, OutBufferSize);
	}
	else
	{
		strncpy_s(OutBuffer, OutBufferSize, this->m_SQLData[index], (OutBufferSize - 1));
	}
}

void CQueryManager::GetAsBinary(char* ColName, BYTE* OutBuffer, int OutBufferSize)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		memset(OutBuffer, 0, OutBufferSize);
	}
	else
	{
		//this->ConvertStringToBinary(this->m_SQLData[index], sizeof(this->m_SQLData[index]), OutBuffer, OutBufferSize);

		// 获取 BLOB 数据和大小
		const void* blobData = sqlite3_column_blob(this->m_stmt, index);
		int blobSize = sqlite3_column_bytes(this->m_stmt, index);

		// 处理 NULL 或空数据
		if (blobData == nullptr || blobSize == 0)
		{
			OutBufferSize = 0;
			return ;
		}

		// 验证输出缓冲区
		if (OutBuffer == nullptr)
		{
			return ; 
		}

		if (OutBufferSize < blobSize)
		{
			// 设置所需的缓冲区大小
			blobSize = OutBufferSize;
		}

		// 复制数据到输出缓冲区
		memcpy(OutBuffer, blobData, blobSize);
		OutBufferSize = blobSize; // 返回实际复制的字节数
	}
}

void CQueryManager::BindParameterAsString(int ParamNumber, void* InBuffer, int ColumnSize)
{
	if (ParamNumber > 0 && ParamNumber <= MAX_BIND_PARAMS)
	{

		// 计算实际字符串长度
		int actualSize = strlen((char*)InBuffer);// (ColumnSize == -1) ? strlen((char*)InBuffer) : ColumnSize;

		this->m_BindParams[ParamNumber - 1].type = BIND_STRING;
		this->m_BindParams[ParamNumber - 1].buffer = InBuffer;
		this->m_BindParams[ParamNumber - 1].size = actualSize;// ColumnSize;  // 存储实际数据长度（不包括null）
	}
}

void CQueryManager::BindParameterAsBinary(int ParamNumber, void* InBuffer, int ColumnSize)
{
	if (ParamNumber > 0 && ParamNumber <= MAX_BIND_PARAMS)
	{
		this->m_BindParams[ParamNumber - 1].type = BIND_BINARY;
		this->m_BindParams[ParamNumber - 1].buffer = InBuffer;
		this->m_BindParams[ParamNumber - 1].size = ColumnSize;
	}
}

void CQueryManager::ApplyBindings()
{
	if (this->m_stmt == NULL)
	{
		return;
	}

	for (int n = 0; n < MAX_BIND_PARAMS; n++)
	{
		if (this->m_BindParams[n].type == BIND_STRING)
		{
			sqlite3_bind_text(this->m_stmt, n + 1, (const char*)this->m_BindParams[n].buffer, this->m_BindParams[n].size, SQLITE_TRANSIENT);
		}
		else if (this->m_BindParams[n].type == BIND_BINARY)
		{
			// Convert binary data to hex string format for consistent storage and retrieval
			// This matches the MSSQL behavior where binary data is stored as hex text
			// Max binary size is limited by m_SQLData buffer (8192 bytes), so hex needs 16384+1 chars
			//char hexBuffer[16385];
			//int hexLen = this->m_BindParams[n].size * 2;
			//if (hexLen + 1 <= (int)sizeof(hexBuffer))
			//{
			//	this->ConvertBinaryToString((BYTE*)this->m_BindParams[n].buffer, this->m_BindParams[n].size, hexBuffer, hexLen + 1);
			//	sqlite3_bind_text(this->m_stmt, n + 1, hexBuffer, hexLen, SQLITE_TRANSIENT);
			//}
			//else
			//{
			//	LogAdd(LOG_RED, "[QueryManager] 二进制数据过大,无法绑定参数 %d (大小: %d, 最大: 8192)", n + 1, this->m_BindParams[n].size);
			//	sqlite3_bind_null(this->m_stmt, n + 1);
			//}

			if (this->m_stmt != NULL)
			{
				sqlite3_bind_blob(this->m_stmt, n + 1, (BYTE*)this->m_BindParams[n].buffer, this->m_BindParams[n].size, SQLITE_TRANSIENT);
			}
		}
	}

	this->ClearBindings();
}

void CQueryManager::ClearBindings()
{
	memset(this->m_BindParams, 0, sizeof(this->m_BindParams));
}

void CQueryManager::ConvertStringToBinary(char* InBuff, int InSize, BYTE* OutBuff, int OutSize)
{
	int size = 0;

	memset(OutBuff, 0, OutSize);

	int hexCount = 0;

	for (int n = 0; n < InSize && size < OutSize; n++)
	{
		char c = InBuff[n];

		if (c == 0)
		{
			break;
		}

		int val = -1;

		if (c >= '0' && c <= '9') val = c - '0';
		else if (c >= 'A' && c <= 'F') val = c - 'A' + 10;
		else if (c >= 'a' && c <= 'f') val = c - 'a' + 10;
		else continue; // skip any non-hex characters

		if ((hexCount % 2) == 0)
		{
			OutBuff[size] = (BYTE)(val << 4);
		}
		else
		{
			OutBuff[size] |= (BYTE)val;
			size++;
		}

		hexCount++;
	}
}

void CQueryManager::ConvertBinaryToString(BYTE* InBuff, int InSize, char* OutBuff, int OutSize)
{
	int size = 0;

	memset(OutBuff, 0, OutSize);

	for (int n = 0; n < OutSize && size < InSize; n++)
	{
		if ((n % 2) == 0)
		{
			OutBuff[n] = (((InBuff[size] / 16) >= 10) ? ('A' + ((InBuff[size] / 16) - 10)) : ('0' + (InBuff[size] / 16)));

			size = size + 0;
		}
		else
		{
			OutBuff[n] = (((InBuff[size] % 16) >= 10) ? ('A' + ((InBuff[size] % 16) - 10)) : ('0' + (InBuff[size] % 16)));

			size = size + 1;
		}
	}
}

#elif !defined(MYSQL)

CQueryManager gQueryManager;

CQueryManager::CQueryManager()
{
	this->m_SQLEnvironment = SQL_NULL_HANDLE;

	this->m_SQLConnection = SQL_NULL_HANDLE;

	this->m_STMT = SQL_NULL_HANDLE;

	this->m_RowCount = -1;

	this->m_ColCount = -1;

	memset(this->m_SQLColName, 0, sizeof(this->m_SQLColName));

	memset(this->m_SQLData, 0, sizeof(this->m_SQLData));

	SQLAllocHandle(SQL_HANDLE_ENV, SQL_NULL_HANDLE, &this->m_SQLEnvironment);

	SQLSetEnvAttr(this->m_SQLEnvironment, SQL_ATTR_ODBC_VERSION, (SQLPOINTER)SQL_OV_ODBC3, SQL_IS_INTEGER);
}

CQueryManager::~CQueryManager()
{
	this->Disconnect();
}

bool CQueryManager::Connect(char* odbc, char* user, char* pass)
{
	strcpy_s(this->m_odbc, odbc);

	strcpy_s(this->m_user, user);

	strcpy_s(this->m_pass, pass);

	if (SQL_SUCCEEDED(SQLAllocHandle(SQL_HANDLE_DBC, this->m_SQLEnvironment, &this->m_SQLConnection)) == 0)
	{
		return false;
	}

	if (SQL_SUCCEEDED(SQLConnect(this->m_SQLConnection, (SQLCHAR*)this->m_odbc, SQL_NTS, (SQLCHAR*)this->m_user, SQL_NTS, (SQLCHAR*)this->m_pass, SQL_NTS)) == 0)
	{
		return false;
	}

	if (SQL_SUCCEEDED(SQLAllocHandle(SQL_HANDLE_STMT, this->m_SQLConnection, &this->m_STMT)) == 0)
	{
		return false;
	}
	else
	{
		return true;
	}
}

void CQueryManager::Disconnect()
{
	if (this->m_STMT != SQL_NULL_HANDLE)
	{
		SQLFreeHandle(SQL_HANDLE_STMT, this->m_STMT);

		this->m_STMT = SQL_NULL_HANDLE;
	}

	if (this->m_SQLConnection != SQL_NULL_HANDLE)
	{
		SQLFreeHandle(SQL_HANDLE_DBC, this->m_SQLConnection);

		this->m_SQLConnection = SQL_NULL_HANDLE;
	}

	if (this->m_SQLEnvironment != SQL_NULL_HANDLE)
	{
		SQLFreeHandle(SQL_HANDLE_ENV, this->m_SQLEnvironment);

		this->m_SQLEnvironment = SQL_NULL_HANDLE;
	}
}

void CQueryManager::Diagnostic(char* query)
{
	LogAdd(LOG_BLACK, "%s", query);

	SQLINTEGER NativeError;

	SQLSMALLINT RecNumber = 1, BufferLength;

	SQLCHAR SqlState[6], MessageText[SQL_MAX_MESSAGE_LENGTH];

	while (SQLGetDiagRec(SQL_HANDLE_STMT, this->m_STMT, (RecNumber++), SqlState, &NativeError, MessageText, sizeof(MessageText), &BufferLength) != SQL_NO_DATA)
	{
		LogAdd(LOG_RED, "[QueryManager] 状态 (%s), 诊断信息: %s", SqlState, MessageText);
	}

	if (strcmp((char*)SqlState, "08S01") == 0)
	{
		this->Connect(this->m_odbc, this->m_user, this->m_pass);
	}
}

bool CQueryManager::ExecQuery(char* query, ...)
{
	char buff[4096];

	va_list arg;

	va_start(arg, query);

	vsprintf_s(buff, query, arg);

	va_end(arg);

	SQLRETURN result;

	if (SQL_SUCCEEDED((result = SQLExecDirect(this->m_STMT, (SQLCHAR*)buff, SQL_NTS))) == 0 && result != SQL_NO_DATA)
	{
		this->Diagnostic(buff);

		return false;
	}

	SQLRowCount(this->m_STMT, &this->m_RowCount);

	if (this->m_RowCount == 0)
	{
		return true;
	}

	SQLNumResultCols(this->m_STMT, &this->m_ColCount);

	if (this->m_ColCount == 0)
	{
		return true;
	}

	if (this->m_ColCount > MAX_COLUMNS)
	{
		return false;
	}

	memset(this->m_SQLColName, 0, sizeof(this->m_SQLColName));

	memset(this->m_SQLData, 0, sizeof(this->m_SQLData));

	for (int n = 0; n < this->m_ColCount; n++)
	{
		SQLDescribeCol(this->m_STMT, (n + 1), this->m_SQLColName[n], sizeof(this->m_SQLColName[n]), 0, 0, 0, 0, 0);

		SQLBindCol(this->m_STMT, (n + 1), SQL_C_CHAR, this->m_SQLData[n], sizeof(this->m_SQLData[n]), &this->m_SQLDataLen[n]);
	}

	return true;
}

void CQueryManager::Close()
{
	SQLCloseCursor(this->m_STMT);

	SQLFreeStmt(this->m_STMT, SQL_UNBIND);
}

SQLRETURN CQueryManager::Fetch()
{
	return SQLFetch(this->m_STMT);
}

int CQueryManager::FindIndex(char* ColName)
{
	for (int n = 0; n < this->m_ColCount; n++)
	{
		if (_stricmp(ColName, (char*)this->m_SQLColName[n]) == 0)
		{
			return n;
		}
	}

	return -1;
}

int CQueryManager::GetResult(int index)
{
	return atoi(this->m_SQLData[index]);
}

int CQueryManager::GetAsInteger(char* ColName)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		return index;
	}
	else
	{
		return atoi(this->m_SQLData[index]);
	}
}

float CQueryManager::GetAsFloat(char* ColName)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		return (float)index;
	}
	else
	{
		return (float)atof(this->m_SQLData[index]);
	}
}

__int64 CQueryManager::GetAsInteger64(char* ColName)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		return index;
	}
	else
	{
		return _atoi64(this->m_SQLData[index]);
	}
}

void CQueryManager::GetAsString(char* ColName, char* OutBuffer, int OutBufferSize)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		memset(OutBuffer, 0, OutBufferSize);
	}
	else
	{
		strncpy_s(OutBuffer, OutBufferSize, this->m_SQLData[index], (OutBufferSize - 1));
	}
}

void CQueryManager::GetAsBinary(char* ColName, BYTE* OutBuffer, int OutBufferSize)
{
	int index = this->FindIndex(ColName);

	if (index == -1)
	{
		memset(OutBuffer, 0, OutBufferSize);
	}
	else
	{
		this->ConvertStringToBinary(this->m_SQLData[index], sizeof(this->m_SQLData[index]), OutBuffer, OutBufferSize);
	}
}

void CQueryManager::BindParameterAsString(int ParamNumber, void* InBuffer, int ColumnSize)
{
	this->m_SQLBindValue[(ParamNumber - 1)] = SQL_NTS;

	SQLBindParameter(this->m_STMT, ParamNumber, SQL_PARAM_INPUT, SQL_C_CHAR, SQL_VARCHAR, ColumnSize, 0, InBuffer, 0, &this->m_SQLBindValue[(ParamNumber - 1)]);
}

void CQueryManager::BindParameterAsBinary(int ParamNumber, void* InBuffer, int ColumnSize)
{
	this->m_SQLBindValue[(ParamNumber - 1)] = ColumnSize;

	SQLBindParameter(this->m_STMT, ParamNumber, SQL_PARAM_INPUT, SQL_C_BINARY, SQL_VARBINARY, ColumnSize, 0, InBuffer, 0, &this->m_SQLBindValue[(ParamNumber - 1)]);
}

void CQueryManager::ConvertStringToBinary(char* InBuff, int InSize, BYTE* OutBuff, int OutSize)
{
	int size = 0;

	memset(OutBuff, 0, OutSize);

	int hexCount = 0;

	for (int n = 0; n < InSize && size < OutSize; n++)
	{
		char c = InBuff[n];

		if (c == 0)
		{
			break;
		}

		int val = -1;

		if (c >= '0' && c <= '9') val = c - '0';
		else if (c >= 'A' && c <= 'F') val = c - 'A' + 10;
		else if (c >= 'a' && c <= 'f') val = c - 'a' + 10;
		else continue; // skip any non-hex characters

		if ((hexCount % 2) == 0)
		{
			OutBuff[size] = (BYTE)(val << 4);
		}
		else
		{
			OutBuff[size] |= (BYTE)val;
			size++;
		}

		hexCount++;
	}
}

void CQueryManager::ConvertBinaryToString(BYTE* InBuff, int InSize, char* OutBuff, int OutSize)
{
	int size = 0;

	memset(OutBuff, 0, OutSize);

	for (int n = 0; n < OutSize && size < InSize; n++)
	{
		if ((n % 2) == 0)
		{
			OutBuff[n] = (((InBuff[size] / 16) >= 10) ? ('A' + ((InBuff[size] / 16) - 10)) : ('0' + (InBuff[size] / 16)));

			size = size + 0;
		}
		else
		{
			OutBuff[n] = (((InBuff[size] % 16) >= 10) ? ('A' + ((InBuff[size] % 16) - 10)) : ('0' + (InBuff[size] % 16)));

			size = size + 1;
		}
	}
}

#else

#include "Log.h"

#pragma comment(lib,"mysqlcppconn.lib")

CQueryManager gQueryManager;

CQueryManager::CQueryManager()
{
	this->driver = NULL;

	this->con = NULL;

	this->connection_properties.clear();

	this->stmt = NULL;

	this->pstmt = NULL;

	this->res = NULL;

	this->m_StringBuffs.clear();

	this->m_BinaryBuffs.clear();
}

CQueryManager::~CQueryManager()
{
	this->Disconnect();
}

bool CQueryManager::Init(std::string HostName, WORD HostPort, std::string Username, std::string Password, std::string SchemaName)
{
	this->connection_properties["hostName"] = HostName;

	this->connection_properties["port"] = HostPort;

	this->connection_properties["userName"] = Username;

	this->connection_properties["password"] = Password;

	this->connection_properties["schema"] = SchemaName;

	this->connection_properties["OPT_RECONNECT"] = true;

	return this->Connect();
}

bool CQueryManager::Connect()
{
	try
	{
		this->driver = get_driver_instance();

		this->con = this->driver->connect(this->connection_properties);
	}
	catch (sql::SQLException e)
	{
		LogAdd(LOG_RED, "[QueryManager] 连接数据库失败: %s", e.what());

		return false;
	}

	return true;
}

void CQueryManager::Disconnect()
{
	this->driver = NULL;

	if (this->con)
	{
		delete this->con;

		this->con = NULL;
	}

	this->connection_properties.clear();

	this->Close();
}

void CQueryManager::PrepareQuery(std::string query, ...)
{
	char buff[4096];

	va_list arg;

	va_start(arg, query);

	vsprintf_s(buff, query.c_str(), arg);

	va_end(arg);

	this->pstmt = this->con->prepareStatement(buff);

	gLog.Output(LOG_QUERY, "[PrepareQuery] Query prepared: %s", buff);
}

bool CQueryManager::ExecPreparedUpdateQuery()
{
	try
	{
		int Quant = this->pstmt->executeUpdate();

		gLog.Output(LOG_QUERY, "[ExecPreparedUpdateQuery] %d rows updated", Quant);
	}
	catch (sql::SQLException& e)
	{
		this->Diagnostic(e);

		return false;
	}
	catch (...)
	{
		LogAdd(LOG_RED, "[ExecPreparedUpdateQuery] 抛出异常");

		return false;
	}

	return true;
}

bool CQueryManager::ExecPreparedResultQuery()
{
	try
	{
		if (this->pstmt->execute())
		{
			this->res = this->pstmt->getResultSet();

			gLog.Output(LOG_QUERY, "[ExecPreparedResultQuery] Query with results.");
		}
		else
		{
			gLog.Output(LOG_QUERY, "[ExecPreparedResultQuery] No results available.");

			return false;
		}
	}
	catch (sql::SQLException& e)
	{
		this->Diagnostic(e);

		return false;
	}
	catch (...)
	{
		LogAdd(LOG_RED, "[ExecPreparedResultQuery] 抛出异常");

		return false;
	}

	return true;
}

bool CQueryManager::ExecUpdateQuery(std::string query, ...)
{
	char buff[4096];

	va_list arg;

	va_start(arg, query);

	vsprintf_s(buff, query.c_str(), arg);

	va_end(arg);

	try
	{
		this->stmt = this->con->createStatement();

		int Quant = this->stmt->executeUpdate(buff);

		gLog.Output(LOG_QUERY, "[ExecUpdateQuery] %d rows updated. Query: %s", Quant, buff);
	}
	catch (sql::SQLException& e)
	{
		this->Diagnostic(e, buff);

		return false;
	}
	catch (...)
	{
		LogAdd(LOG_RED, "[ExecUpdateQuery] 抛出异常");

		return false;
	}

	return true;
}

bool CQueryManager::ExecResultQuery(std::string query, ...)
{
	char buff[4096];

	va_list arg;

	va_start(arg, query);

	vsprintf_s(buff, query.c_str(), arg);

	va_end(arg);

	try
	{
		this->stmt = this->con->createStatement();

		if (this->stmt->execute(buff))
		{
			this->res = this->stmt->getResultSet();

			gLog.Output(LOG_QUERY, "[ExecResultQuery] Query with results. Query: %s", buff);
		}
		else
		{
			gLog.Output(LOG_QUERY, "[ExecResultQuery] No results available. Query: %s", buff);

			return false;
		}
	}
	catch (sql::SQLException& e)
	{
		this->Diagnostic(e, buff);

		return false;
	}
	catch (...)
	{
		LogAdd(LOG_RED, "[ExecResultQuery] 抛出异常");

		return false;
	}

	return true;
}

bool CQueryManager::ExecQuery(std::string query, ...)
{
	char buff[4096];

	va_list arg;

	va_start(arg, query);

	vsprintf_s(buff, query.c_str(), arg);

	va_end(arg);

	try
	{
		this->stmt = this->con->createStatement();

		if (this->stmt->execute(buff))
		{
			this->res = this->stmt->getResultSet();

			gLog.Output(LOG_QUERY, "[ExecQuery] Query with results. Query: %s", buff);
		}
		else
		{
			__int64 Quant = this->stmt->getUpdateCount();

			gLog.Output(LOG_QUERY, "[ExecQuery] %d rows updated. Query: %s", Quant, buff);

			return (Quant > 0);
		}
	}
	catch (sql::SQLException& e)
	{
		this->Diagnostic(e, buff);

		return false;
	}
	catch (...)
	{
		LogAdd(LOG_RED, "[ExecQuery] 抛出异常");

		return false;
	}

	return true;
}

void CQueryManager::Diagnostic(sql::SQLException& e, char* Query)
{
	if (Query)
	{
		LogAdd(LOG_BLUE, "%s", Query);
	}

	std::string SqlState = e.getSQLState();

	LogAdd(LOG_RED, "[QueryManager] 状态 (%s), 诊断信息: %s", SqlState.c_str(), e.what());

	if (SqlState.compare("HY000") == 0)
	{
		this->Connect();
	}
}

void CQueryManager::Close()
{
	if (this->res)
	{
		delete this->res;

		this->res = NULL;
	}

	if (this->stmt)
	{
		while (this->stmt->getMoreResults());

		delete this->stmt;

		this->stmt = NULL;
	}

	if (this->pstmt)
	{
		while (this->pstmt->getMoreResults());

		delete this->pstmt;

		this->pstmt = NULL;
	}

	this->m_StringBuffs.clear();

	for (std::map<int, std::stringstream*>::iterator itr = this->m_BinaryBuffs.begin(); itr != this->m_BinaryBuffs.end(); itr++)
	{
		delete itr->second;
	}

	this->m_BinaryBuffs.clear();
}

bool CQueryManager::Fetch()
{
	return this->res->next();
}

void CQueryManager::SetAsInteger(int position, int value)
{
	this->pstmt->setInt(position, value);
}

void CQueryManager::SetAsFloat(int position, float value)
{
	this->pstmt->setDouble(position, (double)value);
}

void CQueryManager::SetAsInteger64(int position, __int64 value)
{
	this->pstmt->setInt64(position, value);
}

void CQueryManager::SetAsString(int position, char* InBuffer, int InBufferSize)
{
	this->m_StringBuffs[position] = InBuffer;

	if (this->m_StringBuffs[position].length() > (unsigned int)InBufferSize)
	{
		this->m_StringBuffs[position].resize(InBufferSize);
	}

	this->pstmt->setString(position, this->m_StringBuffs[position]);
}

void CQueryManager::SetAsBinary(int position, unsigned char* InBuffer, int InBufferSize)
{
	std::stringstream* buff = new std::stringstream;

	buff->write((const char*)InBuffer, InBufferSize);

	this->m_BinaryBuffs[position] = buff;

	this->pstmt->setBlob(position, this->m_BinaryBuffs[position]);
}

int CQueryManager::GetAsInteger(std::string ColName)
{
	return this->res->getInt(ColName);
}

float CQueryManager::GetAsFloat(std::string ColName)
{
	return (float)this->res->getDouble(ColName);
}

__int64 CQueryManager::GetAsInteger64(std::string ColName)
{
	return this->res->getInt64(ColName);
}

void CQueryManager::GetAsString(std::string ColName, char* OutBuffer, int OutBufferSize)
{
	strncpy_s(OutBuffer, OutBufferSize, this->res->getString(ColName).c_str(), (OutBufferSize - 1));
}

void CQueryManager::GetAsBinary(std::string ColName, unsigned char* OutBuffer, int OutBufferSize)
{
	char* result = new char[OutBufferSize];

	this->res->getBlob(ColName)->read(result, OutBufferSize);

	memcpy(OutBuffer, result, OutBufferSize);

	delete[] result;
}

void CQueryManager::ConvertStringToBinary(char* InBuff, int InSize, unsigned char* OutBuff, int OutSize)
{
	int size = 0;

	memset(OutBuff, 0, OutSize);

	int hexCount = 0;

	for (int n = 0; n < InSize && size < OutSize; n++)
	{
		char c = InBuff[n];

		if (c == 0)
		{
			break;
		}

		int val = -1;

		if (c >= '0' && c <= '9') val = c - '0';
		else if (c >= 'A' && c <= 'F') val = c - 'A' + 10;
		else if (c >= 'a' && c <= 'f') val = c - 'a' + 10;
		else continue; // skip any non-hex characters

		if ((hexCount % 2) == 0)
		{
			OutBuff[size] = (unsigned char)(val << 4);
		}
		else
		{
			OutBuff[size] |= (unsigned char)val;
			size++;
		}

		hexCount++;
	}
}

void CQueryManager::ConvertBinaryToString(unsigned char* InBuff, int InSize, char* OutBuff, int OutSize)
{
	int size = 0;

	memset(OutBuff, 0, OutSize);

	for (int n = 0; n < OutSize && size < InSize; n++)
	{
		if ((n % 2) == 0)
		{
			OutBuff[n] = (((InBuff[size] / 16) >= 10) ? ('A' + ((InBuff[size] / 16) - 10)) : ('0' + (InBuff[size] / 16)));

			size = size + 0;
		}
		else
		{
			OutBuff[n] = (((InBuff[size] % 16) >= 10) ? ('A' + ((InBuff[size] % 16) - 10)) : ('0' + (InBuff[size] % 16)));

			size = size + 1;
		}
	}
}

#endif