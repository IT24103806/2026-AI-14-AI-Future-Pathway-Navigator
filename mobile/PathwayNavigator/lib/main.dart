import 'package:flutter/material.dart';
import 'career_discovery_screen.dart';
import 'pathway_status_screen.dart';

void main() {
  runApp(const MainApp());
}

class MainApp extends StatelessWidget {
  const MainApp({super.key});

  @override
  Widget build(BuildContext context) {
    return const MaterialApp(
      title: 'PathwayNavigator',
      debugShowCheckedModeBanner: false,
      home: AppRootTabs(),
    );
  }
}

class AppRootTabs extends StatefulWidget {
  const AppRootTabs({super.key});

  @override
  State<AppRootTabs> createState() => _AppRootTabsState();
}

class _AppRootTabsState extends State<AppRootTabs> {
  int _selectedIndex = 0;

  final List<Widget> _pages = const [
    CareerDiscoveryScreen(),
    PathwayStatusScreen(studentId: "demo-student-id"),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: _pages[_selectedIndex],
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: (index) => setState(() => _selectedIndex = index),
        selectedItemColor: const Color(0xFF2563EB),
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.work_outline),
            label: 'Career Discovery',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.verified_outlined),
            label: 'Pathway Status',
          ),
        ],
      ),
    );
  }
}