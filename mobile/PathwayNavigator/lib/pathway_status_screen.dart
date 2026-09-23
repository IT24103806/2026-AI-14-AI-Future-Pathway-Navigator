import 'package:flutter/material.dart';

class PathwayStatusScreen extends StatefulWidget {
  final String studentId;
  const PathwayStatusScreen({super.key, required this.studentId});

  @override
  State<PathwayStatusScreen> createState() => _PathwayStatusScreenState();
}

class _PathwayStatusScreenState extends State<PathwayStatusScreen> {
  final String _status = "Pending";
  final int _feasibilityScore = 75;
  final List<String> _missingSkills = const [
    "Python Foundations",
    "Data Structures",
    "SQL Databases"
  ];
  final String _riskReason =
      "Prerequisite gap: Computing transition requires accredited introductory coursework.";
  final String _counsellorNote =
      "Pathway conditionally flagged. Book a 1-on-1 session or complete remedial courses.";
  DateTime? _bookedDate;

  // Native Device Feature: Consultation Date Picker
  Future<void> _selectConsultationDate() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: DateTime.now().add(const Duration(days: 1)),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 60)),
    );

    if (!mounted || picked == null) return;

    setState(() {
      _bookedDate = picked;
    });

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
            'Session booked for: ${picked.year}-${picked.month.toString().padLeft(2, '0')}-${picked.day.toString().padLeft(2, '0')}'),
        backgroundColor: Colors.teal,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  Color _getStatusColor() {
    switch (_status) {
      case "Approved":
        return const Color(0xFF16A34A);
      case "NeedsRevision":
        return const Color(0xFFD97706);
      default:
        return const Color(0xFF2563EB);
    }
  }

  @override
  Widget build(BuildContext context) {
    final statusColor = _getStatusColor();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Feasibility & Approval'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Status Header Banner
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: statusColor.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: statusColor.withValues(alpha: 0.3)),
              ),
              child: Row(
                children: [
                  Icon(Icons.assignment_turned_in_outlined,
                      color: statusColor, size: 36),
                  const SizedBox(width: 12),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Review Status',
                          style:
                              TextStyle(color: Colors.black54, fontSize: 13)),
                      Text(
                        _status,
                        style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                            color: statusColor),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // AI Feasibility Score Progress
            Card(
              elevation: 1,
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12)),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('Agent 4 Reality Match',
                            style: TextStyle(
                                fontWeight: FontWeight.bold, fontSize: 16)),
                        Text('$_feasibilityScore%',
                            style: const TextStyle(
                                fontWeight: FontWeight.bold,
                                color: Color(0xFF2563EB))),
                      ],
                    ),
                    const SizedBox(height: 10),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: LinearProgressIndicator(
                        value: _feasibilityScore / 100,
                        minHeight: 10,
                        backgroundColor: Colors.grey.shade200,
                        color: _feasibilityScore > 70
                            ? Colors.green
                            : Colors.orange,
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),

            // Risk Assessment Flag Card
            if (_riskReason.isNotEmpty) ...[
              Card(
                color: const Color(0xFFFFF1F2),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10)),
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(Icons.warning_amber_rounded,
                          color: Color(0xFFBE123C)),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('Flagged Entry Risk:',
                                style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    color: Color(0xFF9F1239))),
                            const SizedBox(height: 4),
                            Text(_riskReason,
                                style: const TextStyle(
                                    color: Color(0xFFBE123C), fontSize: 13)),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),
            ],

            // Missing Prerequisites
            const Text('Identified Skill Gaps',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 4,
              children: _missingSkills
                  .map((skill) => Chip(
                        label: Text(skill,
                            style: const TextStyle(fontSize: 12)),
                        avatar: const Icon(Icons.school_outlined, size: 16),
                        backgroundColor: const Color(0xFFEFF6FF),
                      ))
                  .toList(),
            ),
            const SizedBox(height: 20),

            // Counsellor Remarks
            Card(
              color: const Color(0xFFF8FAFC),
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10)),
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Counsellor Feedback / Instruction:',
                        style: TextStyle(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 6),
                    Text(_counsellorNote,
                        style: const TextStyle(
                            color: Color(0xFF475569), fontSize: 13)),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            // Device Feature: Date Picker Button for 1-on-1 Consultation
            FilledButton.icon(
              onPressed: _selectConsultationDate,
              icon: const Icon(Icons.calendar_today_outlined),
              label: Text(_bookedDate == null
                  ? 'Book Counsellor 1-on-1 Session'
                  : 'Session Date: ${_bookedDate!.year}-${_bookedDate!.month.toString().padLeft(2, '0')}-${_bookedDate!.day.toString().padLeft(2, '0')}'),
              style: FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(50),
                backgroundColor: const Color(0xFF2563EB),
              ),
            ),
          ],
        ),
      ),
    );
  }
}