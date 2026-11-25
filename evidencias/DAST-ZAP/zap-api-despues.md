# ZAP Scanning Report

ZAP by [Checkmarx](https://checkmarx.com/).


## Summary of Alerts

| Risk Level | Number of Alerts |
| --- | --- |
| High | 0 |
| Medium | 0 |
| Low | 1 |
| Informational | 4 |




## Insights

| Level | Reason | Site | Description | Statistic |
| --- | --- | --- | --- | --- |
| Medium | Exceeded High | https://costamarindo.onrender.com | Percentage of responses with status code 4xx | 95 % |
| Low | Exceeded High | https://costamarindo.onrender.com | Percentage of slow responses | 98 % |
| Info | Informational | http://costamarindo.onrender.com | Percentage of responses with status code 3xx | 100 % |
| Info | Informational | http://costamarindo.onrender.com | Percentage of endpoints with content type text/html | 100 % |
| Info | Informational | http://costamarindo.onrender.com | Percentage of endpoints with method GET | 100 % |
| Info | Informational | http://costamarindo.onrender.com | Count of total endpoints | 2    |
| Info | Informational | http://costamarindo.onrender.com | Percentage of slow responses | 100 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of responses with status code 2xx | 3 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of responses with status code 3xx | 2 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of endpoints with content type application/json | 72 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of endpoints with content type text/html | 27 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of endpoints with method GET | 38 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of endpoints with method POST | 61 % |
| Info | Informational | https://costamarindo.onrender.com | Count of total endpoints | 36    |







## Alerts

| Name | Risk Level | Number of Instances |
| --- | --- | --- |
| Unexpected Content-Type was returned | Low | 20 |
| A Client Error response code was returned by the server | Informational | 41 |
| Non-Storable Content | Informational | Systemic |
| Re-examine Cache-control Directives | Informational | 2 |
| Storable and Cacheable Content | Informational | 4 |




## Alert Detail



### [ Unexpected Content-Type was returned ](https://www.zaproxy.org/docs/alerts/100001/)



##### Low (High)

### Description

A Content-Type of text/html was returned by the server.
This is not one of the types expected to be returned by an API.
Raised by the 'Alert on Unexpected Content Types' script

* URL: http://costamarindo.onrender.com/api/health
  * Node Name: `http://costamarindo.onrender.com/api/health`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: http://costamarindo.onrender.com/api/leaderboard
  * Node Name: `http://costamarindo.onrender.com/api/leaderboard`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com
  * Node Name: `https://costamarindo.onrender.com`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/5424292872359130505
  * Node Name: `https://costamarindo.onrender.com/5424292872359130505`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api
  * Node Name: `https://costamarindo.onrender.com/api`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/leaderboard%3Flimit=5+AND+1%253D1+--+
  * Node Name: `https://costamarindo.onrender.com/api/leaderboard (limit)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/login
  * Node Name: `https://costamarindo.onrender.com/api/auth/login ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/refresh
  * Node Name: `https://costamarindo.onrender.com/api/auth/refresh ()({refresh_token})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/register
  * Node Name: `https://costamarindo.onrender.com/api/auth/register ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/claim
  * Node Name: `https://costamarindo.onrender.com/api/claim ()({session_id,username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/dialog
  * Node Name: `https://costamarindo.onrender.com/api/dialog ()({text,context,item,kind})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/score
  * Node Name: `https://costamarindo.onrender.com/api/score ()({session_id,score,duration_ms,catches,nonce,signature})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/computeMetadata/v1/
  * Node Name: `https://costamarindo.onrender.com/computeMetadata/v1/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/latest/meta-data/
  * Node Name: `https://costamarindo.onrender.com/latest/meta-data/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/metadata/instance
  * Node Name: `https://costamarindo.onrender.com/metadata/instance ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/metadata/v1
  * Node Name: `https://costamarindo.onrender.com/metadata/v1 ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/opc/v1/instance/
  * Node Name: `https://costamarindo.onrender.com/opc/v1/instance/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/opc/v2/instance/
  * Node Name: `https://costamarindo.onrender.com/opc/v2/instance/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/openstack/latest/meta_data.json
  * Node Name: `https://costamarindo.onrender.com/openstack/latest/meta_data.json ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `text/html`
  * Other Info: ``


Instances: 20

### Solution



### Reference




#### Source ID: 4

### [ A Client Error response code was returned by the server ](https://www.zaproxy.org/docs/alerts/100000/)



##### Informational (High)

### Description

A response code of 422 was returned by the server.
This may indicate that the application is failing to handle unexpected input correctly.
Raised by the 'Alert on HTTP Response Code Error' script

* URL: https://costamarindo.onrender.com/5424292872359130505
  * Node Name: `https://costamarindo.onrender.com/5424292872359130505`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/
  * Node Name: `https://costamarindo.onrender.com/api/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/7626014581684703235
  * Node Name: `https://costamarindo.onrender.com/api/7626014581684703235`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth
  * Node Name: `https://costamarindo.onrender.com/api/auth`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/
  * Node Name: `https://costamarindo.onrender.com/api/auth/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/727342357007483113
  * Node Name: `https://costamarindo.onrender.com/api/auth/727342357007483113`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/actuator/health
  * Node Name: `https://costamarindo.onrender.com/api/auth/actuator/health`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/leaderboard%3Flimit=5+AND+1%253D1+--+
  * Node Name: `https://costamarindo.onrender.com/api/leaderboard (limit)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/leaderboard%3Flimit=http%253A%252F%252Fwww.google.com%252F
  * Node Name: `https://costamarindo.onrender.com/api/leaderboard (limit)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/session
  * Node Name: `https://costamarindo.onrender.com/api/session`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/session/
  * Node Name: `https://costamarindo.onrender.com/api/session/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/session/7546787033351733074
  * Node Name: `https://costamarindo.onrender.com/api/session/7546787033351733074`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `404`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/login
  * Node Name: `https://costamarindo.onrender.com/api/auth/login ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/login
  * Node Name: `https://costamarindo.onrender.com/api/auth/login ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/login/
  * Node Name: `https://costamarindo.onrender.com/api/auth/login/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/logout
  * Node Name: `https://costamarindo.onrender.com/api/auth/logout`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/logout/
  * Node Name: `https://costamarindo.onrender.com/api/auth/logout/`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/refresh
  * Node Name: `https://costamarindo.onrender.com/api/auth/refresh ()({refresh_token})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `401`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/refresh
  * Node Name: `https://costamarindo.onrender.com/api/auth/refresh ()({refresh_token})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/refresh
  * Node Name: `https://costamarindo.onrender.com/api/auth/refresh ()({refresh_token})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/refresh/
  * Node Name: `https://costamarindo.onrender.com/api/auth/refresh/ ()({refresh_token})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/register
  * Node Name: `https://costamarindo.onrender.com/api/auth/register ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/register
  * Node Name: `https://costamarindo.onrender.com/api/auth/register ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/register/
  * Node Name: `https://costamarindo.onrender.com/api/auth/register/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/claim
  * Node Name: `https://costamarindo.onrender.com/api/claim ()({session_id,username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/claim
  * Node Name: `https://costamarindo.onrender.com/api/claim ()({session_id,username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/claim/
  * Node Name: `https://costamarindo.onrender.com/api/claim/ ()({session_id,username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/dialog
  * Node Name: `https://costamarindo.onrender.com/api/dialog ()({text,context,item,kind})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/dialog
  * Node Name: `https://costamarindo.onrender.com/api/dialog ()({text,context,item,kind})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/dialog
  * Node Name: `https://costamarindo.onrender.com/api/dialog ()({text,context,item,kind})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `429`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/dialog/
  * Node Name: `https://costamarindo.onrender.com/api/dialog/ ()({text,context,item,kind})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `429`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/score
  * Node Name: `https://costamarindo.onrender.com/api/score ()({session_id,score,duration_ms,catches,nonce,signature})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `403`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/score
  * Node Name: `https://costamarindo.onrender.com/api/score ()({session_id,score,duration_ms,catches,nonce,signature})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/score/
  * Node Name: `https://costamarindo.onrender.com/api/score/ ()({session_id,score,duration_ms,catches,nonce,signature})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/computeMetadata/v1/
  * Node Name: `https://costamarindo.onrender.com/computeMetadata/v1/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `421`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/latest/meta-data/
  * Node Name: `https://costamarindo.onrender.com/latest/meta-data/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `421`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/metadata/instance
  * Node Name: `https://costamarindo.onrender.com/metadata/instance ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `421`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/metadata/v1
  * Node Name: `https://costamarindo.onrender.com/metadata/v1 ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `421`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/opc/v1/instance/
  * Node Name: `https://costamarindo.onrender.com/opc/v1/instance/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `421`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/opc/v2/instance/
  * Node Name: `https://costamarindo.onrender.com/opc/v2/instance/ ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `421`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/openstack/latest/meta_data.json
  * Node Name: `https://costamarindo.onrender.com/openstack/latest/meta_data.json ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `421`
  * Other Info: ``


Instances: 41

### Solution



### Reference



#### CWE Id: [ 388 ](https://cwe.mitre.org/data/definitions/388.html)


#### WASC Id: 20

#### Source ID: 4

### [ Non-Storable Content ](https://www.zaproxy.org/docs/alerts/10049/)



##### Informational (Medium)

### Description

The response contents are not storable by caching components such as proxy servers. If the response does not contain sensitive, personal or user-specific information, it may benefit from being stored and cached, to improve performance.

* URL: https://costamarindo.onrender.com/api/auth/login
  * Node Name: `https://costamarindo.onrender.com/api/auth/login ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/refresh
  * Node Name: `https://costamarindo.onrender.com/api/auth/refresh ()({refresh_token})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/auth/register
  * Node Name: `https://costamarindo.onrender.com/api/auth/register ()({username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/claim
  * Node Name: `https://costamarindo.onrender.com/api/claim ()({session_id,username,pin})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/score
  * Node Name: `https://costamarindo.onrender.com/api/score ()({session_id,score,duration_ms,catches,nonce,signature})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: `422`
  * Other Info: ``

Instances: Systemic


### Solution

The content may be marked as storable by ensuring that the following conditions are satisfied:
The request method must be understood by the cache and defined as being cacheable ("GET", "HEAD", and "POST" are currently defined as cacheable)
The response status code must be understood by the cache (one of the 1XX, 2XX, 3XX, 4XX, or 5XX response classes are generally understood)
The "no-store" cache directive must not appear in the request or response header fields
For caching by "shared" caches such as "proxy" caches, the "private" response directive must not appear in the response
For caching by "shared" caches such as "proxy" caches, the "Authorization" header field must not appear in the request, unless the response explicitly allows it (using one of the "must-revalidate", "public", or "s-maxage" Cache-Control response directives)
In addition to the conditions above, at least one of the following conditions must also be satisfied by the response:
It must contain an "Expires" header field
It must contain a "max-age" response directive
For "shared" caches such as "proxy" caches, it must contain a "s-maxage" response directive
It must contain a "Cache Control Extension" that allows it to be cached
It must have a status code that is defined as cacheable by default (200, 203, 204, 206, 300, 301, 404, 405, 410, 414, 501).

### Reference


* [ https://datatracker.ietf.org/doc/html/rfc7234 ](https://datatracker.ietf.org/doc/html/rfc7234)
* [ https://datatracker.ietf.org/doc/html/rfc7231 ](https://datatracker.ietf.org/doc/html/rfc7231)
* [ https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html ](https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html)


#### CWE Id: [ 524 ](https://cwe.mitre.org/data/definitions/524.html)


#### WASC Id: 13

#### Source ID: 3

### [ Re-examine Cache-control Directives ](https://www.zaproxy.org/docs/alerts/10015/)



##### Informational (Low)

### Description

The cache-control header has not been set properly or is missing, allowing the browser and proxies to cache content. For static assets like css, js, or image files this might be intended, however, the resources should be reviewed to ensure that no sensitive content will be cached.

* URL: https://costamarindo.onrender.com/api/health
  * Node Name: `https://costamarindo.onrender.com/api/health`
  * Method: `GET`
  * Parameter: `cache-control`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/api/leaderboard%3Flimit=5
  * Node Name: `https://costamarindo.onrender.com/api/leaderboard (limit)`
  * Method: `GET`
  * Parameter: `cache-control`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``


Instances: 2

### Solution

For secure content, ensure the cache-control HTTP header is set with "no-cache, no-store, must-revalidate". If an asset should be cached consider setting the directives "public, max-age, immutable".

### Reference


* [ https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html#web-content-caching ](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html#web-content-caching)
* [ https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cache-Control ](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cache-Control)
* [ https://grayduck.mn/2021/09/13/cache-control-recommendations/ ](https://grayduck.mn/2021/09/13/cache-control-recommendations/)


#### CWE Id: [ 525 ](https://cwe.mitre.org/data/definitions/525.html)


#### WASC Id: 13

#### Source ID: 3

### [ Storable and Cacheable Content ](https://www.zaproxy.org/docs/alerts/10049/)



##### Informational (Medium)

### Description

The response contents are storable by caching components such as proxy servers, and may be retrieved directly from the cache, rather than from the origin server by the caching servers, in response to similar requests from other users. If the response data is sensitive, personal or user-specific, this may result in sensitive information being leaked. In some cases, this may even result in a user gaining complete control of the session of another user, depending on the configuration of the caching components in use in their environment. This is primarily an issue where "shared" caching servers such as "proxy" caches are configured on the local network. This configuration is typically found in corporate or educational environments, for instance.

* URL: https://costamarindo.onrender.com/api/health
  * Node Name: `https://costamarindo.onrender.com/api/health`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: `In the absence of an explicitly specified caching lifetime directive in the response, a liberal lifetime heuristic of 1 year was assumed. This is permitted by rfc7234.`
* URL: https://costamarindo.onrender.com/api/leaderboard%3Flimit=5
  * Node Name: `https://costamarindo.onrender.com/api/leaderboard (limit)`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: `In the absence of an explicitly specified caching lifetime directive in the response, a liberal lifetime heuristic of 1 year was assumed. This is permitted by rfc7234.`
* URL: https://costamarindo.onrender.com/api/dialog
  * Node Name: `https://costamarindo.onrender.com/api/dialog ()({text,context,item,kind})`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: `In the absence of an explicitly specified caching lifetime directive in the response, a liberal lifetime heuristic of 1 year was assumed. This is permitted by rfc7234.`
* URL: https://costamarindo.onrender.com/api/session/start
  * Node Name: `https://costamarindo.onrender.com/api/session/start`
  * Method: `POST`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: `In the absence of an explicitly specified caching lifetime directive in the response, a liberal lifetime heuristic of 1 year was assumed. This is permitted by rfc7234.`


Instances: 4

### Solution

Validate that the response does not contain sensitive, personal or user-specific information. If it does, consider the use of the following HTTP response headers, to limit, or prevent the content being stored and retrieved from the cache by another user:
Cache-Control: no-cache, no-store, must-revalidate, private
Pragma: no-cache
Expires: 0
This configuration directs both HTTP 1.0 and HTTP 1.1 compliant caching servers to not store the response, and to not retrieve the response (without validation) from the cache, in response to a similar request.

### Reference


* [ https://datatracker.ietf.org/doc/html/rfc7234 ](https://datatracker.ietf.org/doc/html/rfc7234)
* [ https://datatracker.ietf.org/doc/html/rfc7231 ](https://datatracker.ietf.org/doc/html/rfc7231)
* [ https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html ](https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html)


#### CWE Id: [ 524 ](https://cwe.mitre.org/data/definitions/524.html)


#### WASC Id: 13

#### Source ID: 3


