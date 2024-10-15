import requests
from pprint import pprint

aadGraphDomainName = input("Enter graph endpoint domain name (enter localhost if testing locally): ")
tenantId = input("Enter tenant ID: ")
accessToken = input("Enter access token: ")
apiVersion = input("Enter API version: ")
objectId = input("Enter object ID: ")

def constructGraphBaseUrl(tenantId):
    return 'https://%s/v2/%s'%(aadGraphDomainName, tenantId)

def restoreDeletedObject(tenantId, objectId, accessToken):
    graphBaseUrl=constructGraphBaseUrl(tenantId)
    graphUrl = '%s/deletedItems/%s/restore?api-version=%s'%(graphBaseUrl, objectId, apiVersion)
    print(graphUrl)
    authorizationHeaderValue = 'Bearer %s'%(accessToken)
    headers = {'Authorization': authorizationHeaderValue}
    response = requests.post(graphUrl, headers=headers, verify=False)
    pprint(response)

def patchUserPasswordProfile(tenantId, userObjectId, accessToken):
    graphBaseUrl=constructGraphBaseUrl(tenantId)
    graphUrl = '%s/users/%s?api-version=%s'%(graphBaseUrl, userObjectId, apiVersion)
    print(graphUrl)
    authorizationHeaderValue = 'Bearer %s'%(accessToken)
    headers = {'Authorization': authorizationHeaderValue}
    # todo : generate some password here
    payload = '{"passwordProfile":{"password":""}}'
    response = requests.patch(graphUrl, json=payload, headers=headers, verify=False)
    pprint(response)

#restoreDeletedObject(tenantId, objectId, accessToken)
patchUserPasswordProfile(tenantId, objectId, accessToken)
