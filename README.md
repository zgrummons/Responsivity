# Responsivity API
===========

## Overview
The Responsivity API provides a service to any internal application that calls it. It does nothing except email people if you fail to continue calling the service. It solves the problem of mysteriously dying VM/container services.

## Endpoints
* UpsertApplicationByBody 
  * POST Responsivity/Applications
  * Updates or creates a task using JSON information in the request body
* UpsertApplicationByPath 
  * POST Responsivity/Applications/{application name}
  * Updates or creates a task with default parameters and pulling the application name from the path
* ListApplication 
  * GET Responsivity/Applications/
  * Returns task information for a specified application
* ListApplications 
  * GET Responsivity/Applications/{application name}
  * Returns task information for all applications
* CancelApplicationByBody 
  * DELETE Responsivity/Applications/
  * Cancels running task using JSON information in the request body
* CancelApplicationByPath 
  * DELETE Responsivity/Applications/{application name}
  * Cancels running task using application name from path

## Getting Started
You start by calling one of the upsert endpoints to start service, use the JSON body endpoints if you don't want default options (15 minute timer, emails Zachariah Grummons, default should probably be changed to application support team). Once started, you must continue to POST or cancel the task before the timer runs out; otherwise people are going to get emails.  
To stop the service, use the DELETE method.

### Sample:
POST https://apim-dev.nationalwesternlife.com/enterpriseservices/responsivity/v1/Applications
```
{
    "ApplicationName": "testBody",
    "DelayMilliseconds": 900000,
    "MailAddresses": 
    {
        "test@test.com": "Test Test"
    }
}
```