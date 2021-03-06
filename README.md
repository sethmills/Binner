# Binner
[![GitHub release](https://img.shields.io/github/release/replaysMike/Binner.svg)](https://GitHub.com/replaysMike/Binner/releases/)
[![GitHub commits](https://img.shields.io/github/commits-since/replaysMike/Binner/v1.0.1.svg)](https://GitHub.com/replaysMike/Binner/commit/)
[![Github all releases](https://img.shields.io/github/downloads/replaysMike/Binner/total.svg)](https://GitHub.com/replaysMike/Binner/releases/)
[![GitHub contributors](https://img.shields.io/github/contributors/replaysMike/Binner.svg)](https://GitHub.com/replaysMike/Binner/graphs/contributors/)
[![GitHub license](https://img.shields.io/github/license/replaysMike/Binner.svg)](https://github.com/replaysMike/Binner/blob/master/LICENSE)

Binner is a free open-source parts inventory tracking system you can run locally in Windows or Unix environments. It was created for makers (like myself), hobbyists or professionals to keep track of your parts inventory.

# Screenshots

![Binner](https://github.com/replaysMike/Binner/wiki/binner-v1.png)

## Features
* Standard inventory management input
* Import your orders from Digikey, Mouser or ~AliExpress~
* Customizable nested category placement
* Track parts by Project
* Automated datasheet retrieval / Datasheet search
* Automated part lookup on Digikey/Mouser
* Flexible search engine
* Export your data to CSV / Excel if you need
* Proprietary file-based database (or use providers for other formats such as SQL Server)
* No web server installation required, uses standalone Kestrel service API
* Based on .Net Core - runs on Windows and Unix
* Simple web-based UI
* Barcoding support
* Label printing

## Planned Upcoming Features
* ~~Barcode scanning~~ now supported!
* Dedicated datasheet repository
* Windows desktop UI
* Maybe a parts marketplace?

## Installation

Download the [latest release](https://github.com/replaysMike/Binner/releases) for your environment.

Binner can be run as a standalone console application, or as a service.

To run as a console application:

```ps
.\Binner.Web.exe
```

To run as a service, run the following in an Administrative console:

```ps
.\Binner.Web install
.\Binner.Web start
```

Other commands are available to manage the service such as `uninstall`, `stop`, `help`.

Proceed to the web interface at http://localhost:8090 to start using Binner!

## Description

Binner is designed for electronics in mind, however it could be used for other types of inventory management (chemistry, retail). It is purpose built for quick data entry and fast performance. I built it because I couldn't find good free alternatives for tracking inventory in my home maker lab - it's easy to forget what you bought and where you put them and I end up sometimes ordering things I already have around. Saves money and time! It's built on .Net Core / C# with a React js front end.

I welcome all who want to contribute to this project, and please suggest features in the Issues section.

## What if I want to use it but not bother to set it up myself?

I will be launching a free online version at [Binner.io)](http://binner.io) which should be available around October 2020. This will have both free (max 100 parts) or $20/year for unlimited. Compared to other options out there it's incredibly inexpensive for makers!

## Storage Provider

Binner supports multiple storage providers. If your needs are simple, you can continue to use the built-in `Binner` storage provider which is configured by default. Alternatively, you can use SQL Server as your data provider if you have SQL Server installed.

### Configuring Binner Provider (default)
In `appsettings.json` under `StorageProviderConfiguration` set the `Provider` to `Binner` and under `ProviderConfiguration` specify the database file location as `"Filename": "C:\\Binner\\binner.db"` or the location of your choice. It should look like the following:

```json
"StorageProviderConfiguration": {
    "Provider": "Binner",
    "ProviderConfiguration": {
      "Filename": "C:\\Binner\\binner.db"
    }
  },
 ```

### Configuring SQL Server Provider

In `appsettings.json` under `StorageProviderConfiguration` set the `Provider` to `SqlServer` and under `ProviderConfiguration` specify the connection string as `"ConnectionString": "Server=localhost;Database=Binner;Trusted_Connection=True;"`. It should look like the following:

```json
"StorageProviderConfiguration": {
    "Provider": "SqlServer",
    "ProviderConfiguration": {
      "ConnectionString": "Server=localhost;Database=Binner;Trusted_Connection=True;"
    }
  },
 ```

## Integrations

Currently supports _Digikey_, _Mouser_, _Octopart_ and ~_AliExpress_~. For standalone installations, you will need to obtain your own API keys to enable these features. It's easy to obtain them but be aware Octopart _is not free_ so you may want to avoid using it.

Integrations enable features such as automatic part metadata lookup, datasheet retrieval and automatic importing parts from your orders. To get the best out of Binner, it is a good idea to sign up for Digikey and Mouser API keys at a minimum however they are not required.

### Configuring DigiKey API

Visit [https://developer.digikey.com/](https://developer.digikey.com/) and sign up for a free developer account. You will be asked to create an App which will come with a `ClientId` and `ClientSecret` and needs to be set in the `appsettings.json` under the DigiKey configuration section.

*Creating an App*
* The API uses oAuth with postbacks so they will want you to provide an `OAuth Callback`. This can be safely set to `http://localhost:8090/Authorization/Authorize`. If you are not familiar with oAuth, Digikey will call this URL when you successfully authenticate with DigiKey. It is not called by their servers, but rather by the web UI so it does not need to resolve to an external IP. It does need to be set exactly the same in Binner's `oAuthPostbackUrl` in `appsettings.json` otherwise the API calls will not work, as this value must match on both ends.

* You will want to enable API access for the `Product Information` and `Order Support` APIs.

*Sandbox*
If you wish to use the DigiKey sandbox rather than their production API, you can specify the `ApiUrl` to use `https://sandbox-api.digikey.com`. Otherwise, you can leave it set to `https://api.digikey.com`

### Configuring Mouser API

Visit [https://www.mouser.com/api-hub/](https://www.mouser.com/api-hub/) and sign up for a free developer account. Mouser requires you to sign up for each API product you wish to use. Currently, Binner supports both the `Search API` and `Order API` so sign up for those two APIs separately. Once you have an API key for each, set those in the `appsettings.json` under the Mouser configuration section.

### Configuring Octopart API

Visit [https://octopart.com/api/home](https://octopart.com/api/home) and sign up for a developer account. Please note that Octopart API _is not free_ to use so you may opt to skip this one. They don't advertise pricing until you start using the API (sneaky), but if you already have a key it can be used for additional datasheet support. If you do not wish to use it Digikey and Mouser will be used to access datasheets for parts, as well as the free Binner datasheet API.

## Label printing

Binner currently has limited label printing support. It works best with a Dymo LabelWriter 450 series printer (Turbo dual label is supported too), model 30346 or 30277 labels. If desired additional label sizes and other printers can be easily added.

### Label Example
![Binner](https://github.com/replaysMike/Binner/wiki/binner-label.png)
