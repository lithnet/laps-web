![](https://github.com/lithnet/laps-web/wiki/images/logo-ex-small.png)
# Lithnet LAPS Web App
The Lithnet LAPS Web App is an IIS application that allows you to manage access to local admin passwords that are managed by the [Microsoft Local Admin Password Solution (LAPS)](https://technet.microsoft.com/en-us/mt227395.aspx)

It provides granular permissions, auditing, email alerting and rate-limited access to LAPS passwords stored in a directory. 

It is compatible with OpenID Connect, WS-Federation (ADFS), and integrated windows authentication.

### Screen shots
#### Requesting a password
The LAPS web app provides a simple interface for accessing local admin passwords. Simply provide the computer name, and if you have access, the password is shown.

![](https://github.com/lithnet/laps-web/wiki/images/RequestPassword.png)

Administrators also have the option of setting an expiry time when a password is accessed. This ensures that the password is rotated after use.

![](https://github.com/lithnet/laps-web/wiki/images/ShowPassword.png)

#### Audit success and failure event logs
All success and failure events are logged to the event log

![](https://github.com/lithnet/laps-web/wiki/images/AuditSuccess.png)

![](https://github.com/lithnet/laps-web/wiki/images/AuditFail.png)

#### Rate limiting
To prevent mass enumeration of passwords, you can limit the number of passwords an IP address or user can access within a given period.

![](https://github.com/lithnet/laps-web/wiki/images/RateLimited.png)

### Guides
*   [Installing the app](https://github.com/lithnet/laps-web/wiki/Installing-the-app)
*   [Configuration settings](https://github.com/lithnet/laps-web/wiki/Configuration-settings)
*   [Authentication options](https://github.com/lithnet/laps-web/wiki/Authentication-options)
*   [Branding and customisation](https://github.com/lithnet/laps-web/wiki/Branding-and-customisation)

### Download the app
Download the [current release](https://github.com/lithnet/laps-web/releases/)

### How can I contribute to the project
Found an issue?
*   [Log it](https://github.com/lithnet/laps-web/issues)

Want to fix an issue?
*   Clone the project and submit a pull request

### Keep up to date
*   [Visit my blog](http://blog.lithiumblue.com)
*   [Follow me on twitter](https://twitter.com/RyanLNewington)![](http://twitter.com/favicon.ico)
