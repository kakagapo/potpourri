token="..."
url="..."
auth_header_value="Authorization: Bearer $token"

curl -v -X GET -H "$auth_header_value" -H "Content-Type: application/json" -k "$url"

curl -v -X POST -H "$auth_header_value" -H "Content-Type: application/json" -d @.\payload.json -k "$url"

curl -v -X PATCH -H "$auth_header_value" -H "Content-Type: application/json" -d @.\payload.json -k "$url"
