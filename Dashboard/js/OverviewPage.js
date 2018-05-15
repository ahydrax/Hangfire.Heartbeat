var format = function() { return ""; }

var createGraph = function (elementName) {
    var graph = new Rickshaw.Graph({
        element: document.getElementById(elementName),
        height: 400,
        renderer: 'area',
        interpolation: 'cardinal',
        unstack: true,
        series: new Rickshaw.Series.FixedDuration([{ name: "stub" }], undefined, {
            timeInterval: 3000,
            maxDataPoints: 20,
            timeBase: new Date().getTime() / 60000
        })
    });

    var ticksTreatment = 'glow';
    var xAxis = new Rickshaw.Graph.Axis.Time({
        graph: graph,
        ticksTreatment: ticksTreatment,
        timeFixture: new Rickshaw.Fixtures.Time.Local()
    });

    xAxis.render();

    var yAxis = new Rickshaw.Graph.Axis.Y({
        graph: graph,
        tickFormat: Rickshaw.Fixtures.Number.formatKMBT,
        ticksTreatment: ticksTreatment
    });

    yAxis.render();

    return graph;
}

window.onload = function () {
    var cpuGraph = createGraph("cpu-chart");
    var memGraph = createGraph("mem-chart");

    var updater = function () {
        $.get(document.URL + '/stats',
            function (data) {

                var cpuGraphData = {};
                var memGraphData = {};
                for (var i = 0; i < data.length; i++) {
                    var current = data[i];

                    var formattedCpuUsage = numeral(current.cpuUsagePercentage).format('0.00') + '%';
                    var formattedWorkingMemorySet = numeral(current.workingMemorySet).format('0.00b');

                    if ($('#' + current.name).length) {

                        $('#cpu-' + current.name).text(formattedCpuUsage);
                        $('#mem-' + current.name).text(formattedWorkingMemorySet);
                    } else {
                        $('#overview-table').append("<tr id='" + current.name + "'>" +
                            "<td>" + current.name + "</td>" +
                            "<td id='cpu-" + current.name + "'></td>" +
                            "<td id='mem-" + current.name + "'></td>" +
                            "</tr>");

                        $('#cpu-' + current.name).text(formattedCpuUsage);
                        $('#mem-' + current.name).text(formattedWorkingMemorySet);
                    }

                    cpuGraphData[current.name] = current.cpuUsagePercentage;
                    memGraphData[current.name] = current.workingMemorySet;
                }

                cpuGraph.series.addData(cpuGraphData);
                cpuGraph.render();

                memGraph.series.addData(memGraphData);
                memGraph.render();
            });
    };

    updater();
    setInterval(updater, 3000);
}
