# SQLite Database Setup for MuOnline97

## Overview
This folder contains the configuration and database files for running MuOnline with SQLite database instead of MSSQL or MySQL.

SQLite is a lightweight, serverless database that stores all data in a single file. This makes it ideal for:
- Development and testing environments
- Single server setups
- Quick deployment without external database server dependencies

## Setup Instructions

### 1. Download SQLite Library
Before building, download the complete SQLite amalgamation from https://www.sqlite.org/download.html:
- Download the "sqlite-amalgamation" package
- Replace the `sqlite3.h` in `Source/MuServer/Dependencies/sqlite3/` with the complete header
- Add `sqlite3.c` to the same directory
- Or alternatively, link against a pre-built SQLite library

### 2. Create the Database
Use a SQLite client (like sqlite3 command line tool or DB Browser for SQLite) to run the CreateDatabase.sql script:

```bash
sqlite3 MuOnline.db < CreateDatabase.sql
```

Or open DB Browser for SQLite, create a new database named `MuOnline.db`, and execute the SQL script.

### 3. Configure the Servers
The `DataBasePath` setting in the INI files should point to your SQLite database file.

**JoinServer.ini:**
```ini
[DataBaseInfo]
DataBasePath=.\DB\MuOnline.db
```

**DataServer.ini:**
```ini
[DataBaseInfo]
DataBasePath=.\DB\MuOnline.db
```

### 4. Building the Servers
When building from source, select the `Debug_SQLite` or `Release_SQLite` configuration in Visual Studio.

## Differences from MSSQL/MySQL

1. **No Stored Procedures**: SQLite does not support stored procedures. The application handles all database logic directly.

2. **Data Types**: SQLite uses dynamic typing and has fewer data types than MSSQL/MySQL. The schema has been adapted accordingly.

3. **Single File**: All database data is stored in a single `.db` file, making backup and restore very simple.

4. **No Server Required**: SQLite runs directly within the application process, eliminating the need for a separate database server.

## File Locations

- `DB/MuOnline.db` - The SQLite database file (created by running CreateDatabase.sql)
- `DB/CreateDatabase.sql` - SQL script to create the database schema
- `JoinServer/JoinServer.ini` - JoinServer configuration
- `DataServer/DataServer.ini` - DataServer configuration
