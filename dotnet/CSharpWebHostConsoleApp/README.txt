Run the following command to register http address:
===================================================
netsh http add urlacl url=http://+:18080/ user=Everyone

Other netsh commands that might come in handy:
==============================================
netsh http delete urlacl url=https://+:18080/

If you don't do this you will get an error like the one given below.
An exception occurred: HTTP could not register URL http://+:18080/. Your process does not have access rights to this namespace (see http://go.microsoft.com/fwlink/?LinkId=70353 for details).

Goto: http://localhost:18080/root

Once the service is started you can access the WSDL file for the service using the following URL
http://localhost:18080/root?wsdl

