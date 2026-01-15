                                 How to publish and deploy asp.net mvc application

1.	Prepare your Environment:
    Install IIS:
         Go to Control Panel ->Programs-> Turns Windows Feature on or Off
Select  IIS and check essential Web management tools and world wide web
Service is checked 
Click Ok to install
Install .net hosting bundle
Install latest .net sdk from 

https://dotnet.microsoft.com/en-us/download/dotnet/8.0



 

 


 

Additional:
 


In Control panel image looks like I also installed runtime desktop application runtime that’s why one more bundle added 
 

Install it on IIS Server:
   Check IIS Configurations
   Open IIS Manager (run inetmgr)
  Ensure Application pools have required .NET CLR version for your app.

2.	Publish the ASP.NET core MVC Application:

 Open your project in Visual Studio
Go to build -> Publish {Your Application}
Configure Publish Settings
Choose Folder where you want to publish the app.

 


3.	Configure Website on IIS:
      Add Application Pool 
 
Add Website provide physical Path and then run application in browser.
 


Additional Notes:

Database Connection String :
//without ebcryption
    "ShopConnectionString": "Server=DESKTOP-8OJAL6H\\SQLEXPRESS01;Database=InventorySystemShop;User Id=sa;Password=abcd@1234;TrustServerCertificate=True;",


    //               Local  onnection strings for development
    "ShopConnectionString": "Server=DESKTOP-8OJAL6H\\SQLEXPRESS01;Database=InventorySystemShop;Trusted_Connection=True;Integrated Security=True;TrustServerCertificate=True;",

