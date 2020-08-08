# Snitz MVC
 Snitz MVC version 2

 MVC.Net port of the classic ASP Snitz Forums 2000

 [Demo site here](https://www.reddick.co.uk/mvc)

 The code should now also build on Linux using MonoDevelop 7.8.4 and Mono 6.10.0
 Tested on a Raspberry Pi 4 Model B Rev 1.4 8GB running Raspberry Pi OS 64bit beta
 [Demo site here](https://mono.themediawizards.com)

 # Lzma libs

For some reason under Mono it installed two Lzam dll files 'Lzma.dll' and 'Lzma#.dll'.
When running on windows you should remove the 'Lzma.dll' from the www\bin folder. 
When running on Linux + Mono you should add both 'Lzma.dll' and 'Lzma#.dll' to the www\bin folder.

# Web cong files

In the solution route there are two web.config files, one for Microsoft SQL server and on for MySql. 
Just rename the one you require by removing the .mssql or .mysql extensions and copy it to the www folder.

# Database connection

In the www folder open the connectionstrings.config file in your prefered editor.
There are example connection strings for different database types, uncomment the two that relate to your database and remove the others. Add all the
relevent connection details for your database.

# Mail configuration

In the www folder open the mail.config file in your prefered editor.

Fill in all the details required.
For the port, you can use 25, 587 or 465 select the one that your mail server host recommends.
