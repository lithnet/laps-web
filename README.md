![](https://github.com/lithnet/laps-web/wiki/images/logo-ex-small.png)
# Lithnet LAPS Web App is now Lithnet Access Manager!
[Lithnet Access Manager](https://lithnet.io/products/access-manager) (AMS) is the next generation of Lithnet LAPS web. Lithnet Access Manager provides all the functionality of LAPS web, and more! This guide will explain the key differences between the products, and how to get started on your upgrade journey.

### User experience
LAPS web users will feel a sense of familiarity with the Access Manager interface - while we have freshened-up the user interface, the experience is kept similar to LAPS web to minimize the organization change impact of upgrading to AMS.
We've added features like showing a phonetic breakdown of the password (great for reading passwords out over the phone) and reading the password aloud using the text-to-speech engine of the browser.

Our products have been first and foremost designed to enhance the experience of support staff in the field, and that experience continues with AMS.

### Administration and configuration experience
We've ditched the need for IIS completely, so AMS runs as a standalone service. Along with a single installer executable, this drastically reduces the complexity of the setup and upgrade process.

One of the biggest benefits to LAPS web administrators is that the dreaded config file has now been replaced with an intuitive configuration user interface. All configuration is now done through the configuration tool. 

Instead of fighting with XML, you're now able to use the familiar built-in security editor to assign permissions. 

We've rebuilt the authorization engine to solve common complaints about LAPS web, such as the computer not being able to be part of multiple targets, or errors that occur when organizational units used in targets are removed from the directory.

You'll find support for new things like smart card authentication, sending audit notifications to Slack and Microsoft Teams, and scripts automatically generated by the application to help you configure things like permission delegation in AD.

We know you'll just love the new features and configuration experience!

### Licensing
Access Manager comes in two editions - Standard and Enterprise edition. Standard edition is free for all organizations, while enterprise edition is a paid product. 

However, **all scenarios supported by LAPS web, continue to be free in the Standard Edition of Access Manager**. LAPS web users can upgrade to AMS Standard edition, without any loss of functionality. In fact, the standard edition of AMS brings many new features that were not available in LAPS web, including support for accessing BitLocker recovery keys, and providing just-in-time administrative access to Windows computers.

Enterprise edition offers features that were never available in LAPS web, and includes a dedicated support offering.

See our [comparison guide](https://lithnet.io/ams-features) for more information on the differences between the standard and enterprise edition offerings.

### Migration
Ready to get started?

1. First, download the latest edition of Access Manager from the [downloads](https://lithnet.io/products/access-manager/downloads) page.
2. We recommend starting with a new server to install AMS on. Otherwise, you may run into contention over the use of the web server ports between LAPS web and IIS and the new product. 
3. [Install and configure the Access Manager Service](https://docs.lithnet.io/ams/installation/getting-started). You'll need to re-setup some things like the authentication provider, email setup, and UI options.
4. Finally, you can automatically import all your authorization rules directly from your LAPS web config file, using the [import wizard](https://docs.lithnet.io/ams/configuration/importing/importing-rules-from-lithnet-laps-web-app)

If you run into any issues, you can log an [issue here on GitHub](https://github.com/lithnet/access-manager/issues/new) for support

### Support for LAPS Web
The Lithnet LAPS Web product is no longer actively supported. The repository and wiki will remain available for historical purposes. If you experience any issues with LAPS web, please migrate to Access Manager, and if the issue is still not solved, then please raise an [issue on the Access Manager GitHub page](https://github.com/lithnet/access-manager/issues/new)

Organizations that have licensed Access Manager Enterprise edition are eligible for a fixed-term support for LAPS web, while they transition to Access Manager.
