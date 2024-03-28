import urllib.parse
queryStr = "$select=id,accountEnabled,userPrincipalName,displayName,netId,alternativeSecurityIds&$filter=alternativeSecurityIds/any(x:x/type%20eq%205%20and%20x/identityProvider%20eq%20null%20and%20x/key%20eq%20binary'EAMpGL2Jv7c=')"
query_string_dict = urllib.parse.parse_qs(queryStr)
print(query_string_dict)
