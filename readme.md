# Simple SQLServer backup tool

A simple C# utility project for creating backups of SQL Server databases on a schedule. The settings specify the names of the databases for which you want to create a backup, as well as the save directory. Old databases are automatically deleted.

The Settings block:

```csharp
/* Local SQL Server connecton string */
static string connectionString="Server=localhost;Integrated Security=True";

//Optional:connect using credentials
//static string connectionString = "Server=localhost;user id=user2018;password=MYDBPASSWORD";
```
Here you specify the Connection String to the database. In the simplest case, if you use a local server with the default settings, the first line is enough. If you want to connect to the server with login and password, uncomment and modify the second line.
```csharp
/* Database names to backup */

static string[] saDatabases = newstring[] { &quot;shop&quot;, &quot;frontend&quot;, &quot;accounting&quot; };
```
This is an array with the database names on the server, for which you want to make a backup. If you need to make a backup of one database, you can do this:
```csharp
static string[] saDatabases = newstring[] { &quot;shop&quot; };
```

```csharp
/* Backup directory. Please note: Files older than DeletionDays old will be deleted automatically */
static string backupDir = @&quot;C:\DB\_Backups&quot;;
```


This is the name of the directory for storing backups. **Please note that this directory should be used only for storing backups and not for anything else. Why? Because the files from this directory will be deleted after a certain period of rotation of the backup copies** (see below)
```csharp
/* Delete backups older than DeletionDays. Set this to 0 to never delete backups */
static int DeletionDays = 10;
```

After the program is configured and compiled in the Release version, it is necessary to copy it together with the libraries in a separate directory, for example, C:\SimpleDbBackup, also create a directory for the databases (in this example it&#39;s C:\DB\_Backups)

Now you need to create a periodic task with Windows Task Scheduler so that it runs on a schedule every day or twice a day, as you like. I suggest now you to create a task manually. Perhaps in the future I will add the function of creating a scheduled task in the code of the main program, but for now, I propose you do it manually.

I will be happy with any comments and suggestions. If you need some some **custom software** to be developed, feel free to [contact me](https://iq.direct/contacts.html "Custom Software Development - IQ Direct").

Good luck!