import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

/// Base URL for the ASP.NET Core backend (same API the React app calls).
///
/// - Android emulator: the host machine's localhost is reachable at 10.0.2.2.
/// - iOS simulator / desktop / web: use 'http://localhost:5081/api'.
/// - A physical device: replace with your machine's LAN IP, e.g.
///   'http://192.168.1.23:5081/api'.
const String _apiBaseUrl = 'http://10.0.2.2:5081/api';

/// Agent 2 (Career Discovery) screen: run the analysis against the signed-in
/// student's saved profile, show Path A/B/C, and approve/reject the result.
///
/// This screen is self-contained (including its own minimal login) rather
/// than depending on shared app-wide auth/navigation infrastructure, since
/// that doesn't exist yet in this Flutter project.
class CareerDiscoveryScreen extends StatefulWidget {
  const CareerDiscoveryScreen({super.key});

  @override
  State<CareerDiscoveryScreen> createState() => _CareerDiscoveryScreenState();
}

class _CareerDiscoveryScreenState extends State<CareerDiscoveryScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  String? _token;
  bool _isLoggingIn = false;
  String? _loginError;

  Map<String, dynamic>? _result;
  bool _isAnalyzing = false;
  
  bool _isDeciding = false;
  String? _errorMessage;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    setState(() {
      _isLoggingIn = true;
      _loginError = null;
    });
    try {
      final response = await http.post(
        Uri.parse('$_apiBaseUrl/auth/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'email': _emailController.text.trim(),
          'password': _passwordController.text,
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        setState(() {
          _token = data['token'] as String?;
        });
      } else {
        setState(() {
          _loginError = _tryExtractMessage(response.body) ?? 'Login failed. Check your email and password.';
        });
      }
    } catch (_) {
      setState(() {
        _loginError = 'Network error. Could not reach the server.';
      });
    } finally {
      setState(() {
        _isLoggingIn = false;
      });
    }
  }

  Future<void> _analyze() async {
    setState(() {
      _isAnalyzing = true;
      _errorMessage = null;
    });
    try {
      final response = await http.post(
        Uri.parse('$_apiBaseUrl/career-discovery/analyze'),
        headers: {'Authorization': 'Bearer $_token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          _result = jsonDecode(response.body) as Map<String, dynamic>;
        });
      } else {
        setState(() {
          _errorMessage = _tryExtractMessage(response.body) ?? 'Analysis failed (HTTP ${response.statusCode}).';
        });
      }
    } catch (_) {
      setState(() {
        _errorMessage = 'Network error. Could not reach the Career Discovery service.';
      });
    } finally {
      setState(() {
        _isAnalyzing = false;
      });
    }
  }

  Future<void> _decide(String action) async {
    final id = _result?['id'];
    if (id == null) return;

    setState(() {
      _isDeciding = true;
      _errorMessage = null;
    });
    try {
      final response = await http.patch(
        Uri.parse('$_apiBaseUrl/career-discovery/$id/$action'),
        headers: {'Authorization': 'Bearer $_token'},
      );

      if (response.statusCode == 200) {
        setState(() {
          _result = jsonDecode(response.body) as Map<String, dynamic>;
        });
      } else {
        setState(() {
          _errorMessage = _tryExtractMessage(response.body) ?? 'Failed to $action (HTTP ${response.statusCode}).';
        });
      }
    } catch (_) {
      setState(() {
        _errorMessage = 'Network error. Could not reach the server.';
      });
    } finally {
      setState(() {
        _isDeciding = false;
      });
    }
  }

  String? _tryExtractMessage(String body) {
    try {
      final decoded = jsonDecode(body);
      if (decoded is Map<String, dynamic> && decoded['message'] is String) {
        return decoded['message'] as String;
      }
    } catch (_) {
      // Not JSON — ignore and fall back to a generic message.
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Career Discovery')),
      body: SafeArea(
        child: _token == null ? _buildLoginForm() : _buildDiscoveryBody(),
      ),
    );
  }

  Widget _buildLoginForm() {
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Sign in to run Career Discovery',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Uses the same PathwayNavigator account as the web app.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.grey),
            ),
            const SizedBox(height: 24),
            TextField(
              controller: _emailController,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(labelText: 'Email', border: OutlineInputBorder()),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _passwordController,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Password', border: OutlineInputBorder()),
            ),
            const SizedBox(height: 16),
            if (_loginError != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Text(_loginError!, style: const TextStyle(color: Colors.red), textAlign: TextAlign.center),
              ),
            ElevatedButton(
              onPressed: _isLoggingIn ? null : _login,
              child: _isLoggingIn
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                    )
                  : const Text('Login'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDiscoveryBody() {
    final status = _result?['status'] as String?;
    final recommendations = (_result?['recommendations'] as List?) ?? const [];

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Discover Your Career Pathways',
            style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          const Text(
            'Agent 2 analyzes your saved profile against real career data and returns '
            'your top 3 ranked pathways — Path A, B, and C.',
          ),
          const SizedBox(height: 16),
          if (_errorMessage != null)
            Container(
              padding: const EdgeInsets.all(12),
              margin: const EdgeInsets.only(bottom: 16),
              decoration: BoxDecoration(
                color: Colors.red.shade50,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.red.shade200),
              ),
              child: Text(_errorMessage!, style: TextStyle(color: Colors.red.shade800)),
            ),
          ElevatedButton(
            onPressed: _isAnalyzing ? null : _analyze,
            child: _isAnalyzing
                ? const SizedBox(
                    height: 18,
                    width: 18,
                    child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                  )
                : Text(_result == null ? 'Discover My Pathways' : 'Re-run Career Discovery'),
          ),
          const SizedBox(height: 20),
          if (_result != null && status != 'failed') ...[
            _buildDecisionPanel(status),
            const SizedBox(height: 16),
            for (final rec in recommendations) _buildRecommendationCard(rec as Map<String, dynamic>),
          ],
          if (_result != null && status == 'failed')
            Text(
              'Analysis could not be validated: '
              '${((_result?['validation_errors'] as List?) ?? const []).join(' ')}',
              style: const TextStyle(color: Colors.red),
            ),
        ],
      ),
    );
  }

  Widget _buildDecisionPanel(String? status) {
    if (status == 'approved' || status == 'rejected') {
      final approved = status == 'approved';
      return Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: approved ? Colors.green.shade50 : Colors.red.shade50,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(
          approved ? '✅ You approved this analysis' : '❌ You rejected this analysis',
          style: TextStyle(
            color: approved ? Colors.green.shade800 : Colors.red.shade800,
            fontWeight: FontWeight.bold,
          ),
        ),
      );
    }

    return Row(
      children: [
        Expanded(
          child: ElevatedButton(
            onPressed: _isDeciding ? null : () => _decide('approve'),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
            child: const Text('✅ Approve', style: TextStyle(color: Colors.white)),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: OutlinedButton(
            onPressed: _isDeciding ? null : () => _decide('reject'),
            style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('❌ Reject'),
          ),
        ),
      ],
    );
  }

  Widget _buildRecommendationCard(Map<String, dynamic> rec) {
    final missingSkills = ((rec['missing_skills'] as List?) ?? const []).cast<String>();
    final courses = ((rec['recommended_courses'] as List?) ?? const []).cast<String>();
    final roadmap = ((rec['roadmap'] as List?) ?? const []).cast<Map<String, dynamic>>();
    final trend = rec['trend'] as String? ?? 'stable';
    final matchScore = rec['match_score'] as int? ?? 0;
    final isLive = rec['data_source'] == 'adzuna_live';

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        rec['label'] as String? ?? '',
                        style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.blue, fontSize: 12),
                      ),
                      Text(
                        rec['pathway_name'] as String? ?? '',
                        style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
                ),
                CircleAvatar(
                  radius: 28,
                  backgroundColor: _scoreColor(matchScore).withOpacity(0.15),
                  child: Text(
                    '$matchScore%',
                    style: TextStyle(color: _scoreColor(matchScore), fontWeight: FontWeight.bold),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            _trendChip(trend),
            const SizedBox(height: 14),
            _meterRow('Demand', rec['demand_score'] as int? ?? 0, Colors.green),
            _meterRow('Competition', rec['competition_score'] as int? ?? 0, Colors.orange),
            const SizedBox(height: 6),
            Text(
              isLive ? '🟢 Live Market Data (Adzuna)' : '🧪 Simulated Market Data',
              style: const TextStyle(fontSize: 12, color: Colors.grey),
            ),
            const SizedBox(height: 12),
            Text(rec['reasoning'] as String? ?? ''),
            if (missingSkills.isNotEmpty) ...[
              const SizedBox(height: 14),
              const Text('Skills to Learn', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
              const SizedBox(height: 6),
              Wrap(spacing: 6, runSpacing: 6, children: [for (final s in missingSkills) _tag(s, Colors.orange)]),
            ],
            if (courses.isNotEmpty) ...[
              const SizedBox(height: 14),
              const Text('Recommended Courses', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
              const SizedBox(height: 6),
              Wrap(spacing: 6, runSpacing: 6, children: [for (final c in courses) _tag(c, Colors.purple)]),
            ],
            if (roadmap.isNotEmpty) ...[
              const SizedBox(height: 14),
              const Text('Learning Roadmap', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
              const SizedBox(height: 4),
              for (final step in roadmap)
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text('• ${step['phase']}: ${step['course']}'),
                ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _trendChip(String trend) {
    String label;
    Color color;
    switch (trend) {
      case 'rising':
        label = '📈 Rising demand';
        color = Colors.green;
        break;
      case 'declining':
        label = '📉 Declining demand';
        color = Colors.red;
        break;
      default:
        label = '➡️ Stable demand';
        color = Colors.grey;
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: color.withOpacity(0.12), borderRadius: BorderRadius.circular(16)),
      child: Text(label, style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.bold)),
    );
  }

  Widget _meterRow(String label, int value, Color color) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(label, style: const TextStyle(fontSize: 12)),
              Text('$value/100', style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
            ],
          ),
          const SizedBox(height: 4),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: value / 100,
              minHeight: 6,
              backgroundColor: color.withOpacity(0.15),
              valueColor: AlwaysStoppedAnimation<Color>(color),
            ),
          ),
        ],
      ),
    );
  }

  Widget _tag(String text, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(color: color.withOpacity(0.12), borderRadius: BorderRadius.circular(12)),
      child: Text(text, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }

  Color _scoreColor(int score) {
    if (score >= 70) return Colors.green;
    if (score >= 40) return Colors.orange;
    return Colors.red;
  }
}
