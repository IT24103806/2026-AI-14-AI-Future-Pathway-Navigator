import 'package:flutter/material.dart';

/// Plain-language description of what the app does with personal data and AI.
/// Keep this in sync with the real behaviour of the backend and bump
/// `ConsentStore.currentVersion` whenever it changes materially.
class PrivacyNotice extends StatelessWidget {
  const PrivacyNotice({super.key, this.contact = ''});

  final String contact;

  static const List<(String, String)> sections = [
    (
      'What we collect',
      'Your e-mail address, your academic stage, skills, interests and career ambitions, and - only when '
          'you run a Reality Check - your A/L stream, grades, budget level and current skills.',
    ),
    (
      'Why we collect it',
      'Solely to recommend career pathways, build a learning roadmap and check whether a pathway is '
          'realistic for you. We do not show advertising or sell your data, and the app has no '
          'analytics or tracking SDKs.',
    ),
    (
      'How AI is used',
      'Your profile text is sent to our servers and processed by AI agents (which may call an external '
          'language-model provider). AI output can be inaccurate or biased. It is advice, not a '
          'decision: high-impact pathways are paused until a human counsellor reviews them, and you '
          'can ignore or reject any recommendation.',
    ),
    (
      'Where data is kept',
      'On our backend database. On this phone only your sign-in session is stored, encrypted by the '
          'Android Keystore. Android cloud backup is disabled for the app.',
    ),
    (
      'Your choices',
      'You can withdraw consent and sign out at any time from Settings. To ask for access to, correction '
          'or deletion of your data, contact the programme administrators.',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final (title, body) in sections) ...[
          Semantics(header: true, child: Text(title, style: theme.textTheme.titleMedium)),
          const SizedBox(height: 4),
          Text(body),
          const SizedBox(height: 16),
        ],
        if (contact.isNotEmpty) Text('Privacy contact: $contact', style: theme.textTheme.bodyMedium),
      ],
    );
  }
}
