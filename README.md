# legacy-TiebaStat
## Overview
This is a light-weight service that can log members count, posts count, etc. every definie time in a Baidu Tieba forum based on Baidu Tieba webpage. I programmed it in 2013 and it's a rather simple project.

It can download the first page of specified forums on Baidu Tieba every, for example, 10 minutes, analyze the content, and save some overall info to the output file. Info includes:

* Name of the forum
* Members count
* Threads count
* Signed-in members count

Note this app is a service, so after build the project, you should install it using *installutil.exe* and start it up using Computer Management Console or net start command

The service needs some configuration files, the default path is defined in TiebaStatService.vb as follows:

	Private ConfigurationPath As String = "F:\Dump\Tieba_Stat\config.xml"
	Private OutputPath As String = "F:\Dump\Tieba_Stat\stat.txt"

You can change the path manually, or specify the parameter during the startup of the service as follows:

	-C:ConfigurationPath -O:OutputPath

You can specify it temporarily in Computer Management Console. Unfortunately, I just forgot how to specify the parameter for a service permanently.

## Configuation File
As mentioned above, the service needs a configuration file (config.xml). A sample is as follows :

	<?xml version="1.0" encoding="utf-8"?>
	<config>
	  <interval>600</interval>
	  <timerModule>600</timerModule>
	  <urlTemplate>http://tieba.baidu.com/f?ie=utf-8&amp;kw={0}</urlTemplate>
	  <stat>
		<entry key="GoG">%E7%8C%AB%E5%A4%B4%E9%B9%B0%E7%8E%8B%E5%9B%BD</entry>
		<entry key="WotB">%E7%BB%9D%E5%A2%83%E7%8B%BC%E7%8E%8B</entry>
		<entry key="CWarr">%E7%8C%AB%E6%AD%A6%E5%A3%AB</entry>
		<entry key="Xjtu">%CE%F7%B0%B2%BD%BB%CD%A8%B4%F3%D1%A7</entry>
	  </stat>
	</config>

*interval* specifies the duration between two scan procedure, in seconds,

*timerModule* is specified to ensure that the scan is started when the system time (in seconds since midnight) can be exactly divided by this number. This parameter is set to avert ragged time in log files.

And the url of the pages that'll be repetitively downloaded can be decided using *urlTemplate* specified above, along with the *entry* elements. That is, the configuration file above will cause the following pages be downloaded:

* http://tieba.baidu.com/f?ie=utf-8&kw=%E7%8C%AB%E5%A4%B4%E9%B9%B0%E7%8E%8B%E5%9B%BD
* http://tieba.baidu.com/f?ie=utf-8&kw=%E7%BB%9D%E5%A2%83%E7%8B%BC%E7%8E%8B
* *and so on*

The *key* attribute is only used as a mnemonic and will be copied to output file, so you can set it at will. Better not to duplicate among the entries ^^

## TODO
As you can see, this project is rather simple, and the analyze is only based on plain HTML code search. Maybe a HTML parser is needed.

And based on it... maybe we can do some 大数据 lol...

But before that, gotta finish my graduation project for a bachelor's degree...

CXuesong a.k.a. *forest93*  
2015-3-23
