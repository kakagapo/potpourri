
import msal
import binascii
import os
import pprint 
import webbrowser
from urllib.parse import urlsplit, parse_qs
pp = pprint.PrettyPrinter(indent=4)

# This script is used to get an access tokem using the OAuth 2.0 authorization code flow
# https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow
# following instructions from https://learn.microsoft.com/en-us/entra/msal/python/getting-started/acquiring-tokens

clientId = "" # put your client app ID here
resourceAppId = ""
secret = "" # put your app secret here

resourceTenantId = "xxx.xxx"
authority = "https://login.windows-ppe.net/" + resourceTenantId

state = binascii.hexlify(os.urandom(20)).decode('utf-8')
print(state)
scopes = [ resourceAppId + "/.default"];
app = msal.PublicClientApplication(clientId, authority=authority)

response = app.initiate_auth_code_flow(scopes=scopes, redirect_uri="http://localhost", state=state)

if "error" in response:
    print(response.get("error"))
    exit()

#The respose will look something like this
# {   
#    'auth_uri': 'https://login.windows-ppe.net/<tenant id>/oauth2/v2.0/authorize?client_id=...&response_type=code&redirect_uri=http%3A%2F%2Flocalhost&scope=<resource app ID>%2F.default+offline_access+openid+profile&state=...&code_challenge=...&code_challenge_method=S256&nonce=...&client_info=1',
#    'claims_challenge': None,
#    'code_verifier': '...',
#    'nonce': '...',
#    'redirect_uri': 'http://localhost',
#    'scope': [   'profile',
#                 '<resource app ID>/.default',
#                 'openid',
#                 'offline_access'],
#    'state': '...'
# };
pp.pprint(response)

webbrowser.open(response['auth_uri'])

# Format of the URL would be something like this http://localhost/?code=...&client_info=...&state=...&session_state=...#
redirected_url = input("Enter the url you were redirected to:")
query = urlsplit(redirected_url).query

# parse_qs makes created every parameter as a list even if it is a single value, so you need to flatten it
params = parse_qs(query)
flattened_params = {k: v[0] for k, v in params.items()}

pp.pprint(flattened_params)

app = msal.ConfidentialClientApplication (clientId, client_credential=secret, authority=authority)
result = app.acquire_token_by_auth_code_flow(response, flattened_params)

pp.pprint(result)

if "access_token" in result:
    access_token = result["access_token"]
    print("Access token: " + access_token)
else:
    print(result.get("error"))


