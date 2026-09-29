import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import 'auth_common.dart';
import 'login_screen.dart';
import 'otp_screen.dart';


class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key, this.initialUsername = ''});
  final String initialUsername;

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  late final _user = TextEditingController(text: widget.initialUsername);
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _user.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final s = S.of(context);
    if (_user.text.trim().isEmpty) {
      setState(() => _error = s.required);
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final c = await AppScope.read(context).repo.requestPasswordReset(_user.text);
      if (!mounted) return;
      Navigator.of(context).pushReplacement(MaterialPageRoute(builder: (_) => OtpScreen.reset(challenge: c)));
    } catch (e) {
      if (mounted) setState(() => _error = errorText(context, e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return Scaffold(
      appBar: AppBar(title: Text(s.forgotTitle)),
      body: SafeArea(
        child: ContentWidth(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(IdaSpace.s6, IdaSpace.s8, IdaSpace.s6, IdaSpace.s6),
            children: [
              AuthHeader(
                icon: Icons.lock_reset_rounded,
                title: s.forgotTitle,
                step: s.stepOf(1, 3),
                subtitle: s.forgotSubtitle,
              ),
              const SizedBox(height: IdaSpace.s6),
              IdaTextField(
                fieldKey: const Key('forgot-username'),
                label: s.username,
                hint: s.usernameHint,
                controller: _user,
                error: _error,
                prefixIcon: Icons.person_outline_rounded,
                inputFormatters: [UpperCaseFormatter()],
                autofillHints: const [AutofillHints.username],
                onSubmitted: (_) => _submit(),
              ),
              const SizedBox(height: IdaSpace.s4),
              Container(
                padding: const EdgeInsets.all(IdaSpace.s3),
                decoration: BoxDecoration(color: p.accentSoft, borderRadius: IdaRadius.mdR),
                child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Icon(Icons.info_outline_rounded, size: IdaSizes.iconMd, color: p.accentStrong),
                  const SizedBox(width: IdaSpace.s2),
                  Expanded(child: Text(s.forgotEmailUsers, style: context.text.bodyMedium!.copyWith(color: p.accentStrong))),
                ]),
              ),
              const SizedBox(height: IdaSpace.s8),
              PrimaryButton(label: s.sendOtp, icon: Icons.sms_outlined, loading: _busy, onPressed: _submit),
            ],
          ),
        ),
      ),
    );
  }
}
