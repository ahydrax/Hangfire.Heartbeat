var updater = function () {
    $.get(document.URL + '/stats',
        function (data) {
            for (var i = 0; i < data.length; i++) {
                var current = data[i];

                var formattedCpuUsage = numeral(current.cpuUsagePercentage).format('0.00') + ' %';
                var formattedWorkingMemorySet = numeral(current.workingMemorySet).format('0.00b');

                $('#cpu-' + current.name).text(formattedCpuUsage);
                $('#mem-' + current.name).text(formattedWorkingMemorySet);
            }

        });
};

window.onload = function () {
    setInterval(updater, 3000);
}
