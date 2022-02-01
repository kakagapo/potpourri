
# OAuth 2.0 grant types - https://oauth.net/2/grant-types/



# https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow
curl -X POST -H 'Content-Type: application/x-www-form-urlencoded' \
https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token \
-d 'client_id=<client-id>' \
-d 'scope=2ff814a6-3304-4ab8-85cb-cd0e6f879c1d%2F.default' \
-d 'code=<authorization-code>' \
-d 'redirect_uri=<redirect-uri>' \
-d 'grant_type=authorization_code' \
-d 'state=<state>'

# https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth-ropc

# can use 1b730954-1685-4b74-9bfd-dac224a7b894 for client id -> AAD powershell
curl -X POST -H 'Content-Type: application/x-www-form-urlencoded' \
https://login.windows-ppe.net/88a0c796-f8cf-4fdb-a20c-5bc57c40a969/oauth2/token \
-d 'client_id=<client-id>' \
-d 'username=<username>' \
-d 'password=<password>'
-d 'grant_type=password' \
-d 'resource: https://graph.microsoft.com'