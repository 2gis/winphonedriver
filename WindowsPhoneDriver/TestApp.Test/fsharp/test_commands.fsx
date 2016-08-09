//https://github.com/2gis/winphonedriver/wiki/Command-Execute-Script

#r "../../packages/Selenium.WebDriver.2.42.0/lib/net40/WebDriver.dll"

open System
open OpenQA.Selenium
open OpenQA.Selenium.Remote

// from TestApp.Test\py-functional\config.py
let desiredCapabilities () = 
    let capabilities = new DesiredCapabilities()
    let xapPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../../TestApp/Bin/x86/Debug/TestApp_Debug_x86.xap")
    capabilities.SetCapability("app", xapPath)
    capabilities.SetCapability("deviceName", "Emulator 8.1")
    capabilities.IsJavaScriptEnabled <- true
    capabilities
    
let getDriver () =
    new RemoteWebDriver(new Uri("http://localhost:9999"), desiredCapabilities())
    
let driver = getDriver()
driver.ExecuteScript("mobile: invokeMethod", "TestApp.AutomationApi", "Alert", "blah blah")
driver.ExecuteScript("mobile: invokeMethod", "TestApp.AutomationApi", "AlertEmpty")

driver.FindElementById("SetButton").GetAttribute("DataContext.SampleProperty")
driver.FindElementById("SetButton").GetAttribute("Background.Color")
driver.FindElementById("SetButton").GetAttribute("RenderTransform")