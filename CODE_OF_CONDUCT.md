# Code of Conduct

## Our Pledge

We as members, contributors, and leaders pledge to make participation in our community a harassment-free experience for everyone, regardless of age, body size, visible or invisible disability, ethnicity, sex characteristics, gender identity and expression, level of experience, education, socio-economic status, nationality, personal appearance, race, caste, color, religion, or sexual identity and orientation.

We pledge to act and interact in ways that contribute to an open, welcoming, diverse, inclusive, and healthy community.

## Our Standards

### Expected Behavior

Examples of behavior that contributes to a positive environment for our community include:

**Professional Communication**
- Demonstrating empathy and kindness toward other people
- Being respectful of differing opinions, viewpoints, and experiences
- Giving and gracefully accepting constructive feedback
- Accepting responsibility and apologizing to those affected by our mistakes, and learning from the experience
- Focusing on what is best not just for us as individuals, but for the overall community
- Using welcoming and inclusive language

**Technical Collaboration**
- Writing clear, maintainable code that follows project standards
- Providing thorough documentation for features and changes
- Conducting respectful and constructive code reviews
- Helping newcomers understand the codebase and development workflow
- Sharing knowledge and best practices with the community

**IoT and Device Safety**
- Considering the real-world impact of IoT software on devices and end-users
- Prioritizing security, privacy, and reliability in all contributions
- Testing thoroughly to prevent device malfunctions or data breaches
- Documenting potential risks and limitations clearly
- Reporting security vulnerabilities responsibly and privately

### Unacceptable Behavior

Examples of unacceptable behavior include:

**Harassment and Discrimination**
- The use of sexualized language or imagery, and sexual attention or advances of any kind
- Trolling, insulting or derogatory comments, and personal or political attacks
- Public or private harassment
- Publishing others' private information, such as a physical or email address, without their explicit permission
- Other conduct which could reasonably be considered inappropriate in a professional setting

**Technical Misconduct**
- Intentionally introducing bugs, vulnerabilities, or backdoors
- Submitting malicious code or dependencies
- Plagiarizing code without proper attribution
- Ignoring security best practices or privacy considerations
- Bypassing or weakening security features
- Publishing sensitive device credentials, network information, or system configurations

**IoT-Specific Violations**
- Deploying code that could harm connected devices or infrastructure
- Sharing or soliciting unauthorized access to IoT systems
- Creating or distributing malware targeting IoT devices
- Ignoring or dismissing security vulnerabilities in IoT components
- Exposing user data through inadequate privacy protections

## Enforcement Responsibilities

Community leaders are responsible for clarifying and enforcing our standards of acceptable behavior and will take appropriate and fair corrective action in response to any behavior that they deem inappropriate, threatening, offensive, or harmful.

Community leaders have the right and responsibility to remove, edit, or reject comments, commits, code, wiki edits, issues, and other contributions that are not aligned to this Code of Conduct, and will communicate reasons for moderation decisions when appropriate.

## Scope

This Code of Conduct applies within all community spaces, and also applies when an individual is officially representing the community in public spaces. Examples of representing our community include using an official e-mail address, posting via an official social media account, or acting as an appointed representative at an online or offline event.

This Code of Conduct also applies to actions taken outside of these spaces when they have a negative impact on community health.

## Enforcement

### Reporting

Instances of abusive, harassing, or otherwise unacceptable behavior may be reported to the community leaders responsible for enforcement at [INSERT CONTACT EMAIL].

For security vulnerabilities or IoT safety concerns, please use our responsible disclosure process:
1. **Do not** create public issues for security vulnerabilities
2. Email details privately to [INSERT SECURITY EMAIL]
3. Allow reasonable time for assessment and remediation
4. Work collaboratively on disclosure timeline

All complaints will be reviewed and investigated promptly and fairly. All community leaders are obligated to respect the privacy and security of the reporter of any incident.

### Enforcement Guidelines

Community leaders will follow these Community Impact Guidelines in determining the consequences for any action they deem in violation of this Code of Conduct:

#### 1. Correction

**Community Impact**: Use of inappropriate language or other behavior deemed unprofessional or unwelcome in the community.

**Consequence**: A private, written warning from community leaders, providing clarity around the nature of the violation and an explanation of why the behavior was inappropriate. A public apology may be requested.

#### 2. Warning

**Community Impact**: A violation through a single incident or series of actions.

**Consequence**: A warning with consequences for continued behavior. No interaction with the people involved, including unsolicited interaction with those enforcing the Code of Conduct, for a specified period of time. This includes avoiding interactions in community spaces as well as external channels like social media. Violating these terms may lead to a temporary or permanent ban.

#### 3. Temporary Ban

**Community Impact**: A serious violation of community standards, including sustained inappropriate behavior, or introduction of code that poses security or safety risks.

**Consequence**: A temporary ban from any sort of interaction or public communication with the community for a specified period of time. No public or private interaction with the people involved, including unsolicited interaction with those enforcing the Code of Conduct, is allowed during this period. Violating these terms may lead to a permanent ban.

#### 4. Permanent Ban

**Community Impact**: Demonstrating a pattern of violation of community standards, including sustained inappropriate behavior, harassment of an individual, or aggression toward or disparagement of classes of individuals. This also includes intentionally introducing malicious code, backdoors, or security vulnerabilities.

**Consequence**: A permanent ban from any sort of public interaction within the community.

## IoT-Specific Considerations

Given the nature of IoT systems and their potential impact on physical devices and user safety, contributors must:

### Security and Privacy
- Implement secure communication protocols (TLS/SSL for MQTT, HTTPS for APIs)
- Never hardcode credentials, API keys, or sensitive configuration
- Follow the principle of least privilege for device access and permissions
- Implement proper authentication and authorization mechanisms
- Encrypt sensitive data at rest and in transit
- Regularly update dependencies to address security vulnerabilities

### Reliability and Safety
- Test edge cases and failure scenarios thoroughly
- Implement proper error handling and graceful degradation
- Consider the impact of network failures or service interruptions
- Document known limitations and potential failure modes
- Avoid changes that could cause device malfunctions or data loss

### Data Ethics
- Collect only the minimum data necessary for functionality
- Provide clear documentation about what data is collected and why
- Respect user privacy preferences and consent
- Implement data retention policies and secure deletion
- Never sell or share user data without explicit consent

## Attribution

This Code of Conduct is adapted from the [Contributor Covenant][homepage], version 2.1, available at [https://www.contributor-covenant.org/version/2/1/code_of_conduct.html][v2.1].

Community Impact Guidelines were inspired by [Mozilla's code of conduct enforcement ladder][Mozilla CoC].

IoT-specific provisions were added to address the unique security, privacy, and safety considerations of Internet of Things development.

For answers to common questions about this code of conduct, see the FAQ at [https://www.contributor-covenant.org/faq][FAQ]. Translations are available at [https://www.contributor-covenant.org/translations][translations].

[homepage]: https://www.contributor-covenant.org
[v2.1]: https://www.contributor-covenant.org/version/2/1/code_of_conduct.html
[Mozilla CoC]: https://github.com/mozilla/diversity
[FAQ]: https://www.contributor-covenant.org/faq
[translations]: https://www.contributor-covenant.org/translations      
