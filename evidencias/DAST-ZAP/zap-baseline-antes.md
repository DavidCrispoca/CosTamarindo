# ZAP Scanning Report

ZAP by [Checkmarx](https://checkmarx.com/).


## Summary of Alerts

| Risk Level | Number of Alerts |
| --- | --- |
| High | 1 |
| Medium | 2 |
| Low | 3 |
| Informational | 5 |




## Insights

| Level | Reason | Site | Description | Statistic |
| --- | --- | --- | --- | --- |
| Info | Informational | https://costamarindo.onrender.com | Percentage of responses with status code 2xx | 100 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of endpoints with content type image/x-icon | 20 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of endpoints with content type text/css | 20 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of endpoints with content type text/html | 60 % |
| Info | Informational | https://costamarindo.onrender.com | Percentage of endpoints with method GET | 100 % |
| Info | Informational | https://costamarindo.onrender.com | Count of total endpoints | 5    |
| Info | Informational | https://costamarindo.onrender.com | Percentage of slow responses | 100 % |







## Alerts

| Name | Risk Level | Number of Instances |
| --- | --- | --- |
| nginx Version with Known Vulnerabilities | High | 1 |
| CSP: Failure to Define Directive with No Fallback | Medium | 3 |
| CSP: style-src unsafe-inline | Medium | 3 |
| Cross-Origin-Embedder-Policy Header Missing or Invalid | Low | 3 |
| Cross-Origin-Resource-Policy Header Missing or Invalid | Low | 5 |
| Strict-Transport-Security Header Not Set | Low | 4 |
| Information Disclosure - Suspicious Comments | Informational | 6 |
| Modern Web Application | Informational | 3 |
| Re-examine Cache-control Directives | Informational | 3 |
| Storable and Cacheable Content | Informational | 2 |
| Storable but Non-Cacheable Content | Informational | 3 |




## Alert Detail



### [ nginx Version with Known Vulnerabilities ](https://www.zaproxy.org/docs/alerts/20020/)



##### High (Medium)

### Description

The server disclosed the version of a server side technology it is running via an HTTP response header, and that version is affected by known vulnerabilities. The version, header and vulnerabilities, listed by severity, are given in the other info. See the references for the details of each.
Note that some distributions backport security fixes without changing the version number, so check whether the fixes have actually been applied before reporting these as confirmed.

* URL: https://costamarindo.onrender.com/TemplateData/favicon.ico
  * Node Name: `https://costamarindo.onrender.com/TemplateData/favicon.ico`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `nginx/1.26.3`
  * Other Info: `The server disclosed that it is running nginx version 1.26.3 via the "x-render-origin-server" HTTP response header.

31 known vulnerabilities: 0 critical, 4 high, 21 medium, 6 low.

High: CVE-2026-113425, CVE-2026-118899, CVE-2026-123007, CVE-2026-42533

Medium: CVE-2026-120967, CVE-2026-121810, CVE-2026-123633, CVE-2026-123634, CVE-2026-15176, CVE-2026-1642, CVE-2026-27654, CVE-2026-27784, CVE-2026-28753, CVE-2026-32647, CVE-2026-40460, CVE-2026-40701, CVE-2026-42055, CVE-2026-42945, CVE-2026-42946, CVE-2026-56434, CVE-2026-60005, CVE-2026-9256, CVE-2026-95101, CVE-2026-97205, CVE-2026-97629

Low: CVE-2025-53859, CVE-2026-123640, CVE-2026-125283, CVE-2026-27651, CVE-2026-42934, CVE-2026-48142`


Instances: 1

### Solution

Upgrade nginx to a supported version which includes fixes for the listed vulnerabilities. Also configure the server not to disclose its version, e.g. by setting "server_tokens off;".

### Reference


* [ https://nginx.org/en/security_advisories.html ](https://nginx.org/en/security_advisories.html)
* [ https://nginx.org/en/CHANGES ](https://nginx.org/en/CHANGES)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-113425 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-113425)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-118899 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-118899)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-123007 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-123007)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42533 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42533)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-120967 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-120967)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-121810 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-121810)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-123633 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-123633)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-123634 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-123634)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-15176 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-15176)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-1642 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-1642)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-27654 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-27654)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-27784 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-27784)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-28753 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-28753)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-32647 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-32647)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-40460 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-40460)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-40701 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-40701)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42055 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42055)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42945 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42945)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42946 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42946)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-56434 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-56434)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-60005 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-60005)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-9256 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-9256)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-95101 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-95101)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-97205 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-97205)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-97629 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-97629)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2025-53859 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2025-53859)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-123640 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-123640)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-125283 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-125283)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-27651 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-27651)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42934 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-42934)
* [ https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-48142 ](https://www.zaproxy.org/docs/desktop/addons/passive-scan-rules/nginx-known-vulnerabilities/#cve-2026-48142)


#### CWE Id: [ 1395 ](https://cwe.mitre.org/data/definitions/1395.html)


#### WASC Id: 13

#### Source ID: 3

### [ CSP: Failure to Define Directive with No Fallback ](https://www.zaproxy.org/docs/alerts/10055/)



##### Medium (High)

### Description

The Content Security Policy fails to define one of the directives that has no fallback. Missing/excluding them is the same as allowing anything.

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: `content-security-policy`
  * Attack: ``
  * Evidence: `script-src 'self' 'wasm-unsafe-eval' 'sha256-1ntox3z/2O16VrZEMiYZhT/BnU7bFWrGH/JO1/vl80M='; default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; worker-src 'self' blob:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'`
  * Other Info: `The directive(s): form-action is/are among the directives that do not fallback to default-src.`
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: `content-security-policy`
  * Attack: ``
  * Evidence: `script-src 'self' 'wasm-unsafe-eval' 'sha256-1ntox3z/2O16VrZEMiYZhT/BnU7bFWrGH/JO1/vl80M='; default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; worker-src 'self' blob:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'`
  * Other Info: `The directive(s): form-action is/are among the directives that do not fallback to default-src.`
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: `content-security-policy`
  * Attack: ``
  * Evidence: `script-src 'self' 'wasm-unsafe-eval' 'sha256-1ntox3z/2O16VrZEMiYZhT/BnU7bFWrGH/JO1/vl80M='; default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; worker-src 'self' blob:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'`
  * Other Info: `The directive(s): form-action is/are among the directives that do not fallback to default-src.`


Instances: 3

### Solution

Ensure that your web server, application server, load balancer, etc. is properly configured to set the Content-Security-Policy header.

### Reference


* [ https://www.w3.org/TR/CSP/ ](https://www.w3.org/TR/CSP/)
* [ https://caniuse.com/#search=content+security+policy ](https://caniuse.com/#search=content+security+policy)
* [ https://content-security-policy.com/ ](https://content-security-policy.com/)
* [ https://github.com/HtmlUnit/htmlunit-csp ](https://github.com/HtmlUnit/htmlunit-csp)
* [ https://web.dev/articles/csp#resource-options ](https://web.dev/articles/csp#resource-options)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 15

#### Source ID: 3

### [ CSP: style-src unsafe-inline ](https://www.zaproxy.org/docs/alerts/10055/)



##### Medium (High)

### Description

Content Security Policy (CSP) is an added layer of security that helps to detect and mitigate certain types of attacks. Including (but not limited to) Cross Site Scripting (XSS), and data injection attacks. These attacks are used for everything from data theft to site defacement or distribution of malware. CSP provides a set of standard HTTP headers that allow website owners to declare approved sources of content that browsers should be allowed to load on that page — covered types are JavaScript, CSS, HTML frames, fonts, images and embeddable objects such as Java applets, ActiveX, audio and video files.

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: `content-security-policy`
  * Attack: ``
  * Evidence: `script-src 'self' 'wasm-unsafe-eval' 'sha256-1ntox3z/2O16VrZEMiYZhT/BnU7bFWrGH/JO1/vl80M='; default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; worker-src 'self' blob:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'`
  * Other Info: `style-src includes unsafe-inline.`
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: `content-security-policy`
  * Attack: ``
  * Evidence: `script-src 'self' 'wasm-unsafe-eval' 'sha256-1ntox3z/2O16VrZEMiYZhT/BnU7bFWrGH/JO1/vl80M='; default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; worker-src 'self' blob:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'`
  * Other Info: `style-src includes unsafe-inline.`
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: `content-security-policy`
  * Attack: ``
  * Evidence: `script-src 'self' 'wasm-unsafe-eval' 'sha256-1ntox3z/2O16VrZEMiYZhT/BnU7bFWrGH/JO1/vl80M='; default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; worker-src 'self' blob:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'`
  * Other Info: `style-src includes unsafe-inline.`


Instances: 3

### Solution

Ensure that your web server, application server, load balancer, etc. is properly configured to set the Content-Security-Policy header.

### Reference


* [ https://www.w3.org/TR/CSP/ ](https://www.w3.org/TR/CSP/)
* [ https://caniuse.com/#search=content+security+policy ](https://caniuse.com/#search=content+security+policy)
* [ https://content-security-policy.com/ ](https://content-security-policy.com/)
* [ https://github.com/HtmlUnit/htmlunit-csp ](https://github.com/HtmlUnit/htmlunit-csp)
* [ https://web.dev/articles/csp#resource-options ](https://web.dev/articles/csp#resource-options)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 15

#### Source ID: 3

### [ Cross-Origin-Embedder-Policy Header Missing or Invalid ](https://www.zaproxy.org/docs/alerts/90004/)



##### Low (Medium)

### Description

Cross-Origin-Embedder-Policy header is a response header that prevents a document from loading any cross-origin resources that don't explicitly grant the document permission (using CORP or CORS).

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: `Cross-Origin-Embedder-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: `Cross-Origin-Embedder-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: `Cross-Origin-Embedder-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``


Instances: 3

### Solution

Ensure that the application/web server sets the Cross-Origin-Embedder-Policy header appropriately, and that it sets the Cross-Origin-Embedder-Policy header to 'require-corp' for documents.
If possible, ensure that the end user uses a standards-compliant and modern web browser that supports the Cross-Origin-Embedder-Policy header (https://caniuse.com/mdn-http_headers_cross-origin-embedder-policy).

### Reference


* [ https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cross-Origin-Embedder-Policy ](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cross-Origin-Embedder-Policy)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 14

#### Source ID: 3

### [ Cross-Origin-Resource-Policy Header Missing or Invalid ](https://www.zaproxy.org/docs/alerts/90004/)



##### Low (Medium)

### Description

Cross-Origin-Resource-Policy header is an opt-in header designed to counter side-channels attacks like Spectre. Resource should be specifically set as shareable amongst different origins.

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/TemplateData/favicon.ico
  * Node Name: `https://costamarindo.onrender.com/TemplateData/favicon.ico`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/TemplateData/style.css
  * Node Name: `https://costamarindo.onrender.com/TemplateData/style.css`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: `Cross-Origin-Resource-Policy`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``


Instances: 5

### Solution

Ensure that the application/web server sets the Cross-Origin-Resource-Policy header appropriately, and that it sets the Cross-Origin-Resource-Policy header to 'same-origin' for all web pages.
'same-site' is considered as less secured and should be avoided.
If resources must be shared, set the header to 'cross-origin'.
If possible, ensure that the end user uses a standards-compliant and modern web browser that supports the Cross-Origin-Resource-Policy header (https://caniuse.com/mdn-http_headers_cross-origin-resource-policy).

### Reference


* [ https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cross-Origin-Embedder-Policy ](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cross-Origin-Embedder-Policy)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 14

#### Source ID: 3

### [ Strict-Transport-Security Header Not Set ](https://www.zaproxy.org/docs/alerts/10035/)



##### Low (High)

### Description

HTTP Strict Transport Security (HSTS) is a web security policy mechanism whereby a web server declares that complying user agents (such as a web browser) are to interact with it using only secure HTTPS connections (i.e. HTTP layered over TLS/SSL). HSTS is an IETF standards track protocol and is specified in RFC 6797.

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/TemplateData/favicon.ico
  * Node Name: `https://costamarindo.onrender.com/TemplateData/favicon.ico`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``


Instances: 4

### Solution

Ensure that your web server, application server, load balancer, etc. is configured to enforce Strict-Transport-Security.

### Reference


* [ https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Strict_Transport_Security_Cheat_Sheet.html ](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Strict_Transport_Security_Cheat_Sheet.html)
* [ https://owasp.org/www-community/Security_Headers ](https://owasp.org/www-community/Security_Headers)
* [ https://en.wikipedia.org/wiki/HTTP_Strict_Transport_Security ](https://en.wikipedia.org/wiki/HTTP_Strict_Transport_Security)
* [ https://caniuse.com/stricttransportsecurity ](https://caniuse.com/stricttransportsecurity)
* [ https://datatracker.ietf.org/doc/html/rfc6797 ](https://datatracker.ietf.org/doc/html/rfc6797)


#### CWE Id: [ 319 ](https://cwe.mitre.org/data/definitions/319.html)


#### WASC Id: 15

#### Source ID: 3

### [ Information Disclosure - Suspicious Comments ](https://www.zaproxy.org/docs/alerts/10027/)



##### Informational (Medium)

### Description

The response appears to contain suspicious comments which may help an attacker.

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `// user.`
  * Other Info: `The following pattern was used: \bUSER\b and was detected 3 times, the first in likely comment: "// user.", see evidence field for the suspicious comment/snippet.`
* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `his synchronization from`
  * Other Info: `The following pattern was used: \bFROM\b and was detected in likely comment: "// Set this to false if you want to decouple this synchronization from", see evidence field for the suspicious comment/snippet.`
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `// user.`
  * Other Info: `The following pattern was used: \bUSER\b and was detected 3 times, the first in likely comment: "// user.", see evidence field for the suspicious comment/snippet.`
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `his synchronization from`
  * Other Info: `The following pattern was used: \bFROM\b and was detected in likely comment: "// Set this to false if you want to decouple this synchronization from", see evidence field for the suspicious comment/snippet.`
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `// user.`
  * Other Info: `The following pattern was used: \bUSER\b and was detected 3 times, the first in likely comment: "// user.", see evidence field for the suspicious comment/snippet.`
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `his synchronization from`
  * Other Info: `The following pattern was used: \bFROM\b and was detected in likely comment: "// Set this to false if you want to decouple this synchronization from", see evidence field for the suspicious comment/snippet.`


Instances: 6

### Solution

Remove all comments that return information that may help an attacker and fix any underlying problems they refer to.

### Reference



#### CWE Id: [ 615 ](https://cwe.mitre.org/data/definitions/615.html)


#### WASC Id: 13

#### Source ID: 3

### [ Modern Web Application ](https://www.zaproxy.org/docs/alerts/10109/)



##### Informational (Medium)

### Description

The application appears to be a modern web application. If you need to explore it automatically then the Client Spider may well be more effective than the standard one.

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `<script>
      var canvas = document.querySelector("#unity-canvas");

      // Shows a temporary message banner/ribbon for a few seconds, or
      // a permanent error message on top of the canvas if type=='error'.
      // If type=='warning', a yellow highlight color is used.
      // Modify or remove this function to customize the visually presented
      // way that non-critical warnings and error messages are presented to the
      // user.
      function unityShowBanner(msg, type) {
        var warningBanner = document.querySelector("#unity-warning");
        function updateBannerVisibility() {
          warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
        }
        var div = document.createElement('div');
        div.innerHTML = msg;
        warningBanner.appendChild(div);
        if (type == 'error') div.style = 'background: red; padding: 10px;';
        else {
          if (type == 'warning') div.style = 'background: yellow; padding: 10px;';
          setTimeout(function() {
            warningBanner.removeChild(div);
            updateBannerVisibility();
          }, 5000);
        }
        updateBannerVisibility();
      }

      var buildUrl = "Build";
      var loaderUrl = buildUrl + "/webgl.loader.js";
      var config = {
        arguments: [],
        dataUrl: buildUrl + "/webgl.data.gz",
        frameworkUrl: buildUrl + "/webgl.framework.js.gz",
        codeUrl: buildUrl + "/webgl.wasm.gz",
        streamingAssetsUrl: "StreamingAssets",
        companyName: "DefaultCompany",
        productName: "CosTamarindo",
        productVersion: "0.3.0",
        showBanner: unityShowBanner,
        // errorHandler: function(err, url, line) {
        //    alert("error " + err + " occurred at line " + line);
        //    // Return 'true' if you handled this error and don't want Unity
        //    // to process it further, 'false' otherwise.
        //    return true;
        // },
      };

      // By default, Unity keeps WebGL canvas render target size matched with
      // the DOM size of the canvas element (scaled by window.devicePixelRatio)
      // Set this to false if you want to decouple this synchronization from
      // happening inside the engine, and you would instead like to size up
      // the canvas DOM size and WebGL render target sizes yourself.
      // config.matchWebGLToCanvasSize = false;

      // If you would like all file writes inside Unity Application.persistentDataPath
      // directory to automatically persist so that the contents are remembered when
      // the user revisits the site the next time, uncomment the following line:
      // config.autoSyncPersistentDataPath = true;
      // This autosyncing is currently not the default behavior to avoid regressing
      // existing user projects that might rely on the earlier manual
      // JS_FileSystem_Sync() behavior, but in future Unity version, this will be
      // expected to change.

      if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
        // Mobile device style: fill the whole browser client area with the game canvas:

        var meta = document.createElement('meta');
        meta.name = 'viewport';
        meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
        document.getElementsByTagName('head')[0].appendChild(meta);
        document.querySelector("#unity-container").className = "unity-mobile";
        canvas.className = "unity-mobile";

        // To lower canvas resolution on mobile devices to gain some
        // performance, uncomment the following line:
        // config.devicePixelRatio = 1;


      } else {
        // Desktop style: Render the game canvas in a window that can be maximized to fullscreen:
        canvas.style.width = "960px";
        canvas.style.height = "600px";
      }

      document.querySelector("#unity-loading-bar").style.display = "block";

      var script = document.createElement("script");
      script.src = loaderUrl;
      script.onload = () => {
        createUnityInstance(canvas, config, (progress) => {
          document.querySelector("#unity-progress-bar-full").style.width = 100 * progress + "%";
              }).then((unityInstance) => {
                document.querySelector("#unity-loading-bar").style.display = "none";
                document.querySelector("#unity-fullscreen-button").onclick = () => {
                  unityInstance.SetFullscreen(1);
                };

              }).catch((message) => {
                alert(message);
              });
            };

      document.body.appendChild(script);

    </script>`
  * Other Info: `No links have been found while there are scripts, which is an indication that this is a modern web application.`
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `<script>
      var canvas = document.querySelector("#unity-canvas");

      // Shows a temporary message banner/ribbon for a few seconds, or
      // a permanent error message on top of the canvas if type=='error'.
      // If type=='warning', a yellow highlight color is used.
      // Modify or remove this function to customize the visually presented
      // way that non-critical warnings and error messages are presented to the
      // user.
      function unityShowBanner(msg, type) {
        var warningBanner = document.querySelector("#unity-warning");
        function updateBannerVisibility() {
          warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
        }
        var div = document.createElement('div');
        div.innerHTML = msg;
        warningBanner.appendChild(div);
        if (type == 'error') div.style = 'background: red; padding: 10px;';
        else {
          if (type == 'warning') div.style = 'background: yellow; padding: 10px;';
          setTimeout(function() {
            warningBanner.removeChild(div);
            updateBannerVisibility();
          }, 5000);
        }
        updateBannerVisibility();
      }

      var buildUrl = "Build";
      var loaderUrl = buildUrl + "/webgl.loader.js";
      var config = {
        arguments: [],
        dataUrl: buildUrl + "/webgl.data.gz",
        frameworkUrl: buildUrl + "/webgl.framework.js.gz",
        codeUrl: buildUrl + "/webgl.wasm.gz",
        streamingAssetsUrl: "StreamingAssets",
        companyName: "DefaultCompany",
        productName: "CosTamarindo",
        productVersion: "0.3.0",
        showBanner: unityShowBanner,
        // errorHandler: function(err, url, line) {
        //    alert("error " + err + " occurred at line " + line);
        //    // Return 'true' if you handled this error and don't want Unity
        //    // to process it further, 'false' otherwise.
        //    return true;
        // },
      };

      // By default, Unity keeps WebGL canvas render target size matched with
      // the DOM size of the canvas element (scaled by window.devicePixelRatio)
      // Set this to false if you want to decouple this synchronization from
      // happening inside the engine, and you would instead like to size up
      // the canvas DOM size and WebGL render target sizes yourself.
      // config.matchWebGLToCanvasSize = false;

      // If you would like all file writes inside Unity Application.persistentDataPath
      // directory to automatically persist so that the contents are remembered when
      // the user revisits the site the next time, uncomment the following line:
      // config.autoSyncPersistentDataPath = true;
      // This autosyncing is currently not the default behavior to avoid regressing
      // existing user projects that might rely on the earlier manual
      // JS_FileSystem_Sync() behavior, but in future Unity version, this will be
      // expected to change.

      if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
        // Mobile device style: fill the whole browser client area with the game canvas:

        var meta = document.createElement('meta');
        meta.name = 'viewport';
        meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
        document.getElementsByTagName('head')[0].appendChild(meta);
        document.querySelector("#unity-container").className = "unity-mobile";
        canvas.className = "unity-mobile";

        // To lower canvas resolution on mobile devices to gain some
        // performance, uncomment the following line:
        // config.devicePixelRatio = 1;


      } else {
        // Desktop style: Render the game canvas in a window that can be maximized to fullscreen:
        canvas.style.width = "960px";
        canvas.style.height = "600px";
      }

      document.querySelector("#unity-loading-bar").style.display = "block";

      var script = document.createElement("script");
      script.src = loaderUrl;
      script.onload = () => {
        createUnityInstance(canvas, config, (progress) => {
          document.querySelector("#unity-progress-bar-full").style.width = 100 * progress + "%";
              }).then((unityInstance) => {
                document.querySelector("#unity-loading-bar").style.display = "none";
                document.querySelector("#unity-fullscreen-button").onclick = () => {
                  unityInstance.SetFullscreen(1);
                };

              }).catch((message) => {
                alert(message);
              });
            };

      document.body.appendChild(script);

    </script>`
  * Other Info: `No links have been found while there are scripts, which is an indication that this is a modern web application.`
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `<script>
      var canvas = document.querySelector("#unity-canvas");

      // Shows a temporary message banner/ribbon for a few seconds, or
      // a permanent error message on top of the canvas if type=='error'.
      // If type=='warning', a yellow highlight color is used.
      // Modify or remove this function to customize the visually presented
      // way that non-critical warnings and error messages are presented to the
      // user.
      function unityShowBanner(msg, type) {
        var warningBanner = document.querySelector("#unity-warning");
        function updateBannerVisibility() {
          warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
        }
        var div = document.createElement('div');
        div.innerHTML = msg;
        warningBanner.appendChild(div);
        if (type == 'error') div.style = 'background: red; padding: 10px;';
        else {
          if (type == 'warning') div.style = 'background: yellow; padding: 10px;';
          setTimeout(function() {
            warningBanner.removeChild(div);
            updateBannerVisibility();
          }, 5000);
        }
        updateBannerVisibility();
      }

      var buildUrl = "Build";
      var loaderUrl = buildUrl + "/webgl.loader.js";
      var config = {
        arguments: [],
        dataUrl: buildUrl + "/webgl.data.gz",
        frameworkUrl: buildUrl + "/webgl.framework.js.gz",
        codeUrl: buildUrl + "/webgl.wasm.gz",
        streamingAssetsUrl: "StreamingAssets",
        companyName: "DefaultCompany",
        productName: "CosTamarindo",
        productVersion: "0.3.0",
        showBanner: unityShowBanner,
        // errorHandler: function(err, url, line) {
        //    alert("error " + err + " occurred at line " + line);
        //    // Return 'true' if you handled this error and don't want Unity
        //    // to process it further, 'false' otherwise.
        //    return true;
        // },
      };

      // By default, Unity keeps WebGL canvas render target size matched with
      // the DOM size of the canvas element (scaled by window.devicePixelRatio)
      // Set this to false if you want to decouple this synchronization from
      // happening inside the engine, and you would instead like to size up
      // the canvas DOM size and WebGL render target sizes yourself.
      // config.matchWebGLToCanvasSize = false;

      // If you would like all file writes inside Unity Application.persistentDataPath
      // directory to automatically persist so that the contents are remembered when
      // the user revisits the site the next time, uncomment the following line:
      // config.autoSyncPersistentDataPath = true;
      // This autosyncing is currently not the default behavior to avoid regressing
      // existing user projects that might rely on the earlier manual
      // JS_FileSystem_Sync() behavior, but in future Unity version, this will be
      // expected to change.

      if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
        // Mobile device style: fill the whole browser client area with the game canvas:

        var meta = document.createElement('meta');
        meta.name = 'viewport';
        meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
        document.getElementsByTagName('head')[0].appendChild(meta);
        document.querySelector("#unity-container").className = "unity-mobile";
        canvas.className = "unity-mobile";

        // To lower canvas resolution on mobile devices to gain some
        // performance, uncomment the following line:
        // config.devicePixelRatio = 1;


      } else {
        // Desktop style: Render the game canvas in a window that can be maximized to fullscreen:
        canvas.style.width = "960px";
        canvas.style.height = "600px";
      }

      document.querySelector("#unity-loading-bar").style.display = "block";

      var script = document.createElement("script");
      script.src = loaderUrl;
      script.onload = () => {
        createUnityInstance(canvas, config, (progress) => {
          document.querySelector("#unity-progress-bar-full").style.width = 100 * progress + "%";
              }).then((unityInstance) => {
                document.querySelector("#unity-loading-bar").style.display = "none";
                document.querySelector("#unity-fullscreen-button").onclick = () => {
                  unityInstance.SetFullscreen(1);
                };

              }).catch((message) => {
                alert(message);
              });
            };

      document.body.appendChild(script);

    </script>`
  * Other Info: `No links have been found while there are scripts, which is an indication that this is a modern web application.`


Instances: 3

### Solution

This is an informational alert and so no changes are required.

### Reference




#### Source ID: 3

### [ Re-examine Cache-control Directives ](https://www.zaproxy.org/docs/alerts/10015/)



##### Informational (Low)

### Description

The cache-control header has not been set properly or is missing, allowing the browser and proxies to cache content. For static assets like css, js, or image files this might be intended, however, the resources should be reviewed to ensure that no sensitive content will be cached.

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: `cache-control`
  * Attack: ``
  * Evidence: `no-cache`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: `cache-control`
  * Attack: ``
  * Evidence: `no-cache`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: `cache-control`
  * Attack: ``
  * Evidence: `no-cache`
  * Other Info: ``


Instances: 3

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

* URL: https://costamarindo.onrender.com/TemplateData/favicon.ico
  * Node Name: `https://costamarindo.onrender.com/TemplateData/favicon.ico`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `max-age=604800`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/TemplateData/style.css
  * Node Name: `https://costamarindo.onrender.com/TemplateData/style.css`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `max-age=604800`
  * Other Info: ``


Instances: 2

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

### [ Storable but Non-Cacheable Content ](https://www.zaproxy.org/docs/alerts/10049/)



##### Informational (Medium)

### Description

The response contents are storable by caching components such as proxy servers, but will not be retrieved directly from the cache, without validating the request upstream, in response to similar requests from other users.

* URL: https://costamarindo.onrender.com/
  * Node Name: `https://costamarindo.onrender.com/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-cache`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/robots.txt
  * Node Name: `https://costamarindo.onrender.com/robots.txt`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-cache`
  * Other Info: ``
* URL: https://costamarindo.onrender.com/sitemap.xml
  * Node Name: `https://costamarindo.onrender.com/sitemap.xml`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `no-cache`
  * Other Info: ``


Instances: 3

### Solution



### Reference


* [ https://datatracker.ietf.org/doc/html/rfc7234 ](https://datatracker.ietf.org/doc/html/rfc7234)
* [ https://datatracker.ietf.org/doc/html/rfc7231 ](https://datatracker.ietf.org/doc/html/rfc7231)
* [ https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html ](https://www.w3.org/Protocols/rfc2616/rfc2616-sec13.html)


#### CWE Id: [ 524 ](https://cwe.mitre.org/data/definitions/524.html)


#### WASC Id: 13

#### Source ID: 3


