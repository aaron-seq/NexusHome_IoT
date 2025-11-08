# Security Policy

## Supported Versions

We release patches for security vulnerabilities. Currently supported versions:

| Version | Supported          |
| ------- | ------------------ |
| 2.1.x   | :white_check_mark: |
| 2.0.x   | :white_check_mark: |
| < 2.0   | :x:                |

## Reporting a Vulnerability

The NexusHome IoT team takes security vulnerabilities seriously. We appreciate your efforts to responsibly disclose your findings.

### How to Report

Please report security vulnerabilities by emailing **security@nexushome.tech** or directly to **aaron-seq@users.noreply.github.com**

**Do not** report security vulnerabilities through public GitHub issues, discussions, or pull requests.

### What to Include

To help us triage and fix the issue quickly, please include:

- Type of vulnerability (e.g., SQL injection, XSS, authentication bypass)
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit or direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

### Response Timeline

- **Initial Response**: Within 48 hours of report
- **Confirmation**: Within 5 business days
- **Fix Release**: Depends on severity
  - Critical: Within 7 days
  - High: Within 14 days
  - Medium: Within 30 days
  - Low: Next scheduled release

### What to Expect

1. **Acknowledgment**: We will acknowledge receipt of your vulnerability report
2. **Investigation**: We will investigate and confirm the vulnerability
3. **Updates**: We will keep you informed about our progress
4. **Fix**: We will develop and test a fix
5. **Release**: We will release the fix and publicly disclose the vulnerability
6. **Credit**: We will credit you in our security advisories (unless you prefer to remain anonymous)

## Security Best Practices

When deploying NexusHome IoT, follow these security best practices:

### Authentication

- Use strong JWT secret keys (minimum 32 characters)
- Enable multi-factor authentication when available
- Rotate API keys regularly
- Never commit secrets to version control

### Network Security

- Always use HTTPS in production
- Configure proper CORS policies
- Implement rate limiting
- Use firewall rules to restrict access
- Keep MQTT communication encrypted

### Database Security

- Use strong database passwords
- Enable database encryption at rest
- Restrict database access to application servers only
- Regularly backup and encrypt backups
- Keep SQL Server updated with latest security patches

### Application Security

- Keep all dependencies updated
- Run security scans regularly
- Monitor application logs for suspicious activity
- Implement proper input validation
- Use parameterized queries to prevent SQL injection

### Infrastructure Security

- Use container image scanning
- Implement least privilege access
- Enable audit logging
- Regular security audits
- Keep Docker and Kubernetes updated

### IoT Device Security

- Use device certificate authentication
- Implement secure device provisioning
- Monitor device behavior for anomalies
- Regular firmware updates
- Isolate IoT devices on separate network segments

## Known Security Considerations

### Default Configurations

- Change all default passwords before deployment
- Update JWT secret keys from example values
- Configure proper authentication for MQTT broker
- Review and adjust rate limiting thresholds

### Data Protection

- User passwords are hashed using BCrypt
- Sensitive data is encrypted in transit (TLS/SSL)
- Database connections use encrypted channels
- Session tokens expire after configured period

### Third-Party Dependencies

We regularly monitor and update our dependencies for security vulnerabilities using:

- GitHub Dependabot
- NuGet package vulnerability scanning
- Regular security audits

## Security Updates

Security updates are released as:

- **Patch versions** (2.1.x) for backward-compatible security fixes
- **Security advisories** published on GitHub
- **Release notes** documenting security improvements

Subscribe to repository releases to receive notifications about security updates.

## Compliance

NexusHome IoT implements security controls aligned with:

- OWASP Top 10
- CWE/SANS Top 25
- NIST Cybersecurity Framework
- IoT Security Foundation guidelines

## Security Hall of Fame

We recognize security researchers who have helped improve NexusHome IoT security:

*(Contributors will be listed here with their permission)*

## Contact

For security-related questions that are not vulnerabilities:

- Email: support@nexushome.tech
- GitHub Discussions: Security category
- Documentation: /docs/SECURITY_GUIDE.md

## Disclosure Policy

We follow a coordinated disclosure approach:

1. Security issues are fixed privately
2. Patches are released to supported versions
3. Public disclosure occurs after fixes are available
4. Security advisories provide detailed information

## Bug Bounty Program

We are considering establishing a bug bounty program. Stay tuned for updates.

---

**Thank you for helping keep NexusHome IoT and our users secure!**
