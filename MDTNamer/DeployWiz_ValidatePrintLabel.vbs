' // ***************************************************************************
' // 
' // Copyright (c) Almar Daði Björnsson (Djók)
' // 
' // Microsoft Deployment Toolkit Solution Accelerator
' //
' // File:      DeployWiz_ValidatePrintLabel.vbs
' // 
' // Version:   21.87
' // 
' // Purpose:   Velja hvort það eigi að prenta límmiða með nafni tölvu
' // 
' // ***************************************************************************

Option Explicit

'''''''''''''''''''''''''''''''''''''
'  Validate Print Label Pane
'
Function InitializePrintLabel
	'Get values from CS.ini
	PrintLabel.Value = Property("PrintLabel")
	
	'Determine the default value
	If UCase(Property("PrintLabel")) = "YES" then
		DoPrintLabel.checked = true
	Else
		DontPrintLabel.checked = true
	End if
		
	ValidatePrintLabel
	
	
End Function

