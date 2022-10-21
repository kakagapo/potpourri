import requests
from pprint import pprint
import getpass

username = getpass.getuser()
password = getpass.getpass()

tenantId = input("Enter tenant ID: ")

# new endpoint = login.microsoftonline.com and 
# old endpoint = login.windows.net
tokenEndpoint = 'https://login.windows.net/%s/oauth2/token'%(tenantId)

aadGraphDomainName = 'graph.windows.net'
apiVersion = '1.6'
userObjectId = input("Enter user object ID: ")

def setProperty(tenantId, userObjectId, propertyName, propertyValue):
    graphUrl = 'https://%s/%s/users/%s?api-version=%s'%(aadGraphDomainName,tenantId, userObjectId, apiVersion)
    payload = {propertyName: propertyValue}
    authorizationHeaderValue = 'Bearer %s'%(accessToken)
    headers = {'Authorization': authorizationHeaderValue}
    response = requests.patch(graphUrl, json=payload, headers=headers)
    pprint(vars(response.request))
    pprint(vars(response))

payload = {
    'grant_type': 'password',
    'username': username,
    'password': password,
    'client_id': '1b730954-1685-4b74-9bfd-dac224a7b894', # AAD powershell client
    'resource': 'https://%s'%(aadGraphDomainName)
}
x = requests.post(tokenEndpoint, data=payload)
pprint(vars(x.request))
pprint(vars(x))

if(x.status_code == 200):
    responseJson = x.json()
    accessToken = responseJson['access_token']
    # make the required AAD graph call
    setProperty(tenantId, userObjectId, "city", None)
    setProperty(tenantId, userObjectId, "country", "United States")
    




