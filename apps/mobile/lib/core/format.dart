


library;

abstract final class Fmt {
  static const _thMonths = [
    'มกราคม', 'กุมภาพันธ์', 'มีนาคม', 'เมษายน', 'พฤษภาคม', 'มิถุนายน',
    'กรกฎาคม', 'สิงหาคม', 'กันยายน', 'ตุลาคม', 'พฤศจิกายน', 'ธันวาคม',
  ];
  static const _thMonthsShort = [
    'ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.',
    'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.',
  ];
  static const _enMonths = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
  ];
  static const _enMonthsShort = [
    'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
  ];


  static const _thWeekdays = ['จันทร์', 'อังคาร', 'พุธ', 'พฤหัสบดี', 'ศุกร์', 'เสาร์', 'อาทิตย์'];
  static const _thWeekdaysShort = ['จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.', 'อา.'];
  static const _enWeekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  static const _enWeekdaysShort = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  static int year(int ce, bool en) => en ? ce : ce + 543;

  static String month(int m, bool en) => en ? _enMonths[m - 1] : _thMonths[m - 1];
  static String monthShort(int m, bool en) => en ? _enMonthsShort[m - 1] : _thMonthsShort[m - 1];
  static String weekday(int wd, bool en) => en ? _enWeekdays[wd - 1] : _thWeekdays[wd - 1];
  static String weekdayShort(int wd, bool en) => en ? _enWeekdaysShort[wd - 1] : _thWeekdaysShort[wd - 1];


  static String date(DateTime d, bool en) => '${d.day} ${monthShort(d.month, en)} ${year(d.year, en)}';


  static String dateLong(DateTime d, bool en) => '${d.day} ${month(d.month, en)} ${year(d.year, en)}';


  static String dateFull(DateTime d, bool en) => en
      ? '${weekday(d.weekday, en)}, ${dateLong(d, en)}'
      : 'วัน${weekday(d.weekday, en)}ที่ ${dateLong(d, en)}';


  static String dateNumeric(DateTime d, bool en) =>
      '${_pad(d.day)}/${_pad(d.month)}/${year(d.year, en)}';


  static String monthYear(int y, int m, bool en) => '${month(m, en)} ${year(y, en)}';


  static String monthYearShort(int y, int m, bool en) =>
      '${monthShort(m, en)} ${year(y, en).toString().substring(2)}';


  static String time(DateTime d) => '${_pad(d.hour)}:${_pad(d.minute)}';
  static String timeWithSeconds(DateTime d) => '${time(d)}:${_pad(d.second)}';


  static String minuteOfDay(int m) => '${_pad(m ~/ 60)}:${_pad(m % 60)}';


  static String hoursMinutes(Duration d) => '${_pad(d.inHours)}:${_pad(d.inMinutes % 60)}';


  static String durationWords(Duration d, bool en) {
    final h = d.inHours, m = d.inMinutes % 60;
    if (en) return h > 0 ? '$h h $m min' : '$m min';
    return h > 0 ? '$h ชม. $m นาที' : '$m นาที';
  }

  static bool sameDay(DateTime a, DateTime b) => a.year == b.year && a.month == b.month && a.day == b.day;

  static DateTime dayOf(DateTime d) => DateTime(d.year, d.month, d.day);


  static String relative(DateTime d, bool en, {DateTime? now}) {
    final today = dayOf(now ?? DateTime.now());
    final day = dayOf(d);
    final diff = today.difference(day).inDays;
    if (diff == 0) return '${en ? 'Today' : 'วันนี้'} ${time(d)}';
    if (diff == 1) return '${en ? 'Yesterday' : 'เมื่อวาน'} ${time(d)}';
    return '${date(d, en)} ${time(d)}';
  }


  static String money(double value, {bool symbol = false, int decimals = 2}) {
    final fixed = value.abs().toStringAsFixed(decimals);
    final parts = fixed.split('.');
    final digits = parts[0];
    final b = StringBuffer();
    for (var i = 0; i < digits.length; i++) {
      if (i > 0 && (digits.length - i) % 3 == 0) b.write(',');
      b.write(digits[i]);
    }
    final body = decimals > 0 ? '$b.${parts[1]}' : '$b';
    final withSymbol = symbol ? '฿$body' : body;
    return value < 0 ? '($withSymbol)' : withSymbol;
  }


  static String compact(double v) {
    if (v.abs() >= 1000000) return '${(v / 1000000).toStringAsFixed(1)}M';
    if (v.abs() >= 1000) return '${(v / 1000).round()}K';
    return v.round().toString();
  }

  static String percent(double ratio) {
    final v = (ratio * 100).toStringAsFixed(1);
    return ratio > 0 ? '+$v%' : '$v%';
  }


  static String distance(double meters, bool en) {
    if (meters >= 1000) return '${(meters / 1000).toStringAsFixed(1)} ${en ? 'km' : 'กม.'}';
    return '${meters.round()} ${en ? 'm' : 'ม.'}';
  }

  static String fileSize(int kb) => kb >= 1024 ? '${(kb / 1024).toStringAsFixed(1)} MB' : '$kb KB';


  static String maskPhone(String phone) {
    final d = phone.replaceAll(RegExp(r'\D'), '');
    if (d.length < 4) return phone;
    return '${d.substring(0, 3)}-XXX-${d.substring(d.length - 4)}';
  }


  static String maskEmail(String email) {
    final at = email.indexOf('@');
    if (at < 2) return email;
    return '${email.substring(0, 2)}${'•' * (at - 2).clamp(3, 8)}${email.substring(at)}';
  }


  static String maskAccount(String account) {
    final d = account.replaceAll(RegExp(r'\D'), '');
    if (d.length < 5) return account;
    return 'XXX-X-X${d.substring(d.length - 4, d.length - 1)}-${d.substring(d.length - 1)}';
  }

  static String formatAccount(String account) {
    final d = account.replaceAll(RegExp(r'\D'), '');
    if (d.length != 10) return account;
    return '${d.substring(0, 3)}-${d.substring(3, 4)}-${d.substring(4, 9)}-${d.substring(9)}';
  }


  static String maskNationalId(String id) {
    final d = id.replaceAll(RegExp(r'\D'), '');
    if (d.length != 13) return id;
    return '${d[0]}-${d.substring(1, 5)}-XXXXX-XX-${d[12]}';
  }

  static String formatNationalId(String id) {
    final d = id.replaceAll(RegExp(r'\D'), '');
    if (d.length != 13) return id;
    return '${d[0]}-${d.substring(1, 5)}-${d.substring(5, 10)}-${d.substring(10, 12)}-${d[12]}';
  }

  static String _pad(int n) => n.toString().padLeft(2, '0');
}
