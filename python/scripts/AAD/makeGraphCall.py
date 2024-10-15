import requests
from pprint import pprint
import getpass

tenantId = input("Enter tenant ID: ")
username = input("Enter username: ")
password = getpass.getpass()

aadGraphDomainName = 'graph.windows.net'
apiVersion = '1.6'
userObjectId = input("Enter user object ID: ")

def setProperty(tenantId, userObjectId, propertyName, propertyValue, accessToken):
    graphUrl = 'https://%s/%s/users/%s?api-version=%s'%(aadGraphDomainName,tenantId, userObjectId, apiVersion)
    payload = {propertyName: propertyValue}
    authorizationHeaderValue = 'Bearer %s'%(accessToken)
    headers = {'Authorization': authorizationHeaderValue}
    response = requests.patch(graphUrl, json=payload, headers=headers)
    print("Request:")
    pprint(vars(response.request))
    print("Response:")
    pprint(vars(response))

def getToken(tenantId):
    payload = {
        'grant_type': 'password',
        'username': username,
        'password': password,
        'client_id': '1b730954-1685-4b74-9bfd-dac224a7b894', # AAD powershell client
        'resource': 'https://%s'%(aadGraphDomainName)
    }
    tokenEndpoint = 'https://login.windows.net/%s/oauth2/token'%(tenantId)
    tokenResponse = requests.post(tokenEndpoint, data=payload)
    pprint(vars(tokenResponse))
    if(tokenResponse.status_code != 200):
        print("Unable to get token")
        raise
    else:
        responseJson = tokenResponse.json()
        accessToken = responseJson['access_token']

accessToken = getToken(tenantId)

# make the required AAD graph call
setProperty(tenantId, userObjectId, "city", None, accessToken)
setProperty(tenantId, userObjectId, "country", "United States", accessToken)
    




