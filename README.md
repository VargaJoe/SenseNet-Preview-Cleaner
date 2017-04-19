# SenseNet-Preview-Cleaner

## How it works

Preview cleaner tool for Sense/Net ECM. It is used to remove generated previews from the Content Repository (e.g. previews take up too much space or want to regenerate neccessary previews only). 

## Execution workflow

This is what the tool does, when you execute it:

1. loads the top 100 contents with 'PreviewImage' type from the database
2. iterate the contents and remove them from the db one by one using the product own stored procedure: `proc_Node_DeletePhysical`
3. sleep a certain time before continue
4. starts from the beginning

As a result, the tool will delete all the preview images from db and generate some log files about the process.

## Prerequisites

Connection string to the database have to be properly set in `CleanSnPreviews.exe.config`. You can use the setting just like in your site's web.config, for example:
```xml
 <add name="SnCrMsSql" connectionString="Persist Security Info=False;Initial Catalog=SenseNetContentRepository;Data Source=MySenseNetContentRepositoryDatasource;Integrated Security=true" providerName="System.Data.SqlClient" />
```

## Usage

There are a couple of optional parameters by which you can define the behavior of the tool.
```
CleanSnPreviews.exe -MODE Delete -TOP 1000
```

###### Help

You can call the tool with a question mark for get some usage information.
```
CleanSnPreviews.exe -? 
```

###### Show Only

Gets first 100 preview content without removing them(a.k.a. run tool at Show Only mode):
```
CleanSnPreviews -MODE ShowOnly
```

###### Delete

In delete mode the tool will iterate through all the previews by 100 items and delete it. You can change the value of the number by iteration with the parameter below (e.g. delete all preview by 1000):
```
CleanSnPreviews -MODE Delete -TOP 1000
```

###### Config parameters
You can tweak some parameters in `CleanSnPreviews.exe.config` such as:

- `OperationSleep` sets break time in milliseconds between iteration to protect sql server from high load, default value is 2000 
- `LogMaxRowCount` sets a maximum row number for log files (which is placed in the same directory as the program itself), the tool will create a new log if it exceeds. Default value is 100000
- `SqlCommandTimeout` is in seconds and its default value is 120 

## Known issues:

- Display usage shows the program can call with `-help` parameter, but it's not working and only will trigger the tool with default settings.
- Display usage text format is a little dizzy because br tags are not handled.
- There will be a log file even if we've started the tool for usage of information only (with `-?` parameter)
