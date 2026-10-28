# Database setup: Almost New 24/7 Outpost (Windows, local only)

Do this once, before the API agent starts. It takes about 15 minutes. At the end you will have a MySQL server on your PC, the `almost_new_outpost` database with seed data, and a login the API uses.

## Names used everywhere

Use these exact values. The SQL script, the API config and the build prompt all assume them.

| Thing | Value |
| --- | --- |
| MySQL version | **MySQL Community Server 8.4 LTS** (not 9.x Innovation releases) |
| Windows service name | `MySQL84` |
| Host / port | `localhost` / `3306` |
| Admin login | `root` with a password you choose; for setup and resets only |
| Database (dev) | `almost_new_outpost` |
| Database (automated tests) | `almost_new_outpost_test`, which the API test suite creates itself |
| App login (what the API uses) | `outpost_app@localhost` |
| App password | `Outpost_Dev_2026!` (dev only; to change it, see step 6) |
| SQL script | `database/almost_new_outpost.sql` in the API repo |
| Workbench connections | `Outpost Local (root)` and `Outpost Local (app)` |

## 1. Install MySQL Server 8.4 LTS

1. Go to the MySQL Community Downloads page (dev.mysql.com/downloads/mysql/), choose version **8.4.x LTS**, OS **Microsoft Windows**, and download the **MSI Installer** (`mysql-8.4.x-winx64.msi`). Under the download buttons, the "No thanks, just start my download" link skips signing in.
2. Run the MSI and choose **Typical**. When it finishes, leave **Run MySQL Configurator** ticked and click Finish.

## 2. Configure the server (MySQL Configurator)

Go through the Configurator pages with these choices:

| Page | Setting |
| --- | --- |
| Data Directory | Leave the default |
| Type and Networking | Config Type **Development Computer** · TCP/IP **on** · Port **3306** · **untick** "Open Windows Firewall ports" (nothing outside your PC needs it) |
| Accounts and Roles | Set a strong **root** password and save it in your password manager. **Do not add users here**, because the SQL script creates `outpost_app`. |
| Windows Service | Configure as a Windows Service **on** · Service name **`MySQL84`** · Start at system startup: your choice (on is simplest) · Run as Standard System Account |
| Server File Permissions | Leave the default |
| Sample Databases | Don't install them |

Click **Execute**, wait for every step to turn green, then click Finish.

Check it's running: press Win+R, type `services.msc`, find **MySQL84**, and confirm its status is Running.

## 3. Install MySQL Workbench

Download **MySQL Workbench** for Windows (dev.mysql.com/downloads/workbench/) and install it with the defaults.

Create the admin connection: click **+** next to "MySQL Connections" and enter:

- Connection name: `Outpost Local (root)`
- Hostname: `127.0.0.1`, Port: `3306`, Username: `root`
- Click **Test Connection** and enter your root password (you can choose to store it in the vault).

## 4. Run the SQL script

1. Open the `Outpost Local (root)` connection.
2. Go to **File > Open SQL Script…** and choose `almost_new_outpost.sql`.
3. Click the **lightning bolt without a cursor** (Execute all). Don't use "execute current statement", because the stored procedures need the whole file to run.
4. The last result grid should show **Roles 5 · Users 5 · Categories 4 · Products 18 · Purchases 10**.
5. In the Schemas panel, click the refresh icon. `almost_new_outpost` appears with 5 tables, 1 view and 10 stored procedures.

PowerShell alternative (no Workbench). The `mysql.exe` client isn't on your PATH by default, and PowerShell has no `<` redirection:

```powershell
& "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" -u root -p -e "source C:/path/to/almost-new-outpost-api/database/almost_new_outpost.sql"
```

Use forward slashes in the path after `source`.

## 5. Check the app login

Create a second Workbench connection named `Outpost Local (app)` (host `127.0.0.1`, port `3306`, user `outpost_app`, password `Outpost_Dev_2026!`), open it, and run:

```sql
USE almost_new_outpost;
CALL sp_SearchProducts('kit', 'price', 'asc');   -- 4 rows: Cooking Kit, First Aid Kit, Signal Flare Kit, Medical Supply Kit
CALL sp_GetPurchasesByUser(3, 5);                 -- medic's 3 purchases, newest first
DROP TABLE Purchases;                             -- should FAIL: the app login cannot change the schema
```

The first two calls should return rows and the `DROP` should be refused. That confirms the permissions are right.

## 6. Hand the connection details to the API

The API agent stores the connection string in .NET **user-secrets**, never in `appsettings.json`:

```powershell
cd C:\path\to\almost-new-outpost-api\src\AlmostNewOutpost.Api
dotnet user-secrets set "ConnectionStrings:Outpost" "Server=localhost;Port=3306;Database=almost_new_outpost;User ID=outpost_app;Password=Outpost_Dev_2026!;"
```

The API test suite also needs the **root** connection, because it builds and drops `almost_new_outpost_test`. The agent will ask you for it. Give it via user-secrets or an environment variable, never in a committed file.

To use a different app password, change it in **both** `ALTER USER`/`CREATE USER` lines at the top of the SQL script, re-run the script, and update the user-secret.

## Resetting the data

Re-run `almost_new_outpost.sql` as root. It drops and re-creates the database, so every purchase and product edit is lost and the seed data returns (seed purchase times are relative to "now"). The `outpost_app` login is kept.

## Seeded logins

| Username | Password | Role | Can buy |
| --- | --- | --- | --- |
| citizen | citizen123 | Citizen | Basic |
| scout | scout123 | Scout | Basic, Exploration |
| medic | medic123 | Medic | Basic, Medical |
| security | security123 | Security | Basic, Security |
| admin | admin123 | MerchantAdmin | Everything, plus managing products |

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `SHOW TABLES` lists `products`, not `Products` | Normal on Windows (`lower_case_table_names=1`). Names are case-insensitive, so nothing breaks. |
| Error 1064 near `DELIMITER` | You ran one statement instead of the whole file. Use Execute all (step 4.3). |
| "Access denied for user 'outpost_app'" | Re-run the script as root. It resets the app password and its grants. |
| API error mentioning `caching_sha2_password` or SSL | Add `SslMode=Required;` to the connection string. If that doesn't help, add `AllowPublicKeyRetrieval=true;` (fine for localhost dev). |
| Port 3306 already in use | Another MySQL or MariaDB is installed. Stop it in `services.msc`, or reconfigure MySQL84 to port 3307 and use that in the connection string. |
| Forgot the root password | Follow "Resetting the Root Password: Windows Systems" in the MySQL 8.4 Reference Manual. On a fresh dev install, uninstalling and reinstalling is often quicker. |
