# filesender
Small utility for sending files to Lifecycle Management

## Use

Configure the two application settings in App.config:
* **endpointUrl** - the URL to the endpoint recieving the file/files. 
* **filePropertyName** - the name of the field/property which will contain the file/files. This should match the name of the process field in Lifecycle Management.

Debug or run console application with a single argument: the path to the file or folder containing the files you want to send to the endpoint.