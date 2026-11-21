import ipaddress
import re
import unittest

from tools.deliveryctl.runtime import ROOT


class EdgeRealIpTest(unittest.TestCase):
    def test_nginx_trusts_only_the_pinned_frontend_gateway(self):
        conf = (ROOT / 'infra/nginx/nginx.prod.conf').read_text()
        trusted = re.findall(r'^\s*set_real_ip_from\s+([^;\s]+);', conf, re.MULTILINE)
        self.assertEqual(trusted, ['172.29.0.1'])
        self.assertRegex(conf, r'(?m)^\s*real_ip_header\s+X-Forwarded-For;')
        self.assertRegex(conf, r'(?m)^\s*real_ip_recursive\s+off;')
        overlay = (ROOT / 'docker-compose.staging.yml').read_text()
        subnet = re.search(r'frontend-network:\s*\n\s*ipam:\s*\n\s*config:\s*\n\s*- subnet:\s*(\S+)', overlay)
        self.assertIsNotNone(subnet)
        net = ipaddress.ip_network(subnet.group(1))
        self.assertEqual(next(net.hosts()), ipaddress.ip_address(trusted[0]))
        for t in trusted:
            self.assertTrue(ipaddress.ip_address(t).is_private)

    def test_caddy_replaces_x_forwarded_for_with_the_real_peer(self):
        caddy = (ROOT / 'infra/oracle/Caddyfile').read_text()
        self.assertIn('header_up X-Forwarded-For {remote_host}', caddy)
        self.assertNotIn('trusted_proxies', caddy)


if __name__ == '__main__':
    unittest.main()
