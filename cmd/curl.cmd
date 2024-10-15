echo {"displayName": "John Doe"} > payload.json
set token=xxx
set auth_header_value=Authorization: Bearer %token%
set url=...
curl -vk -X PATCH -H "%auth_header_value%" -H "Content-Type: application/json" -d @payload.json "%url%"