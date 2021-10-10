        var GetTopicsByUser = function() {

                $.ajax({
                    type: "POST",
                    url: "/Charts/TopicsByUser",
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (chData) {
                        var aData = chData;
                        var aLabels = aData[0];
                        var aDatasets1 = aData[1];

                        var ctx = $("#chart_topic");

                    var topicChart = new Chart(ctx, {
                        type: 'bar',
                        autoSkip: false,
                        animation:false,
                        showDataPoints:true,
                        responsive: true,
                        data: {
                            labels: aLabels,
                            datasets: [{
                                label: "Topics by User",
                                data: aDatasets1,
                                backgroundColor: 'blue'
                                }]
                        },
                        options: {
                            scaleShowValues: true,
                            scales: {
                                yAxes: [{
                                    ticks: { beginAtZero: true}
                                }],
                                xAxes: [{
                                    ticks: {autoSkip: false }
                                }]
                            }
                        }
                    });
                }
            });
        }

        var GetReplyData = function() {

            $.ajax({
                type: "POST",
                url: "/Charts/RepliesByUser",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (chData) {
                    var aData = chData;
                    var aLabels = aData[0];
                    var aDatasets1 = aData[1];

                    var ctx = $("#chart_reply");
                    var replyChart = new Chart(ctx, {
                        type: 'bar',
                        autoSkip: false,
                        animation:false,
                        showDataPoints:true,
                        responsive: true,
                        data: {
                            labels: aLabels,
                            datasets: [{
                                label: "Replies by User",
                                data: aDatasets1,
                                backgroundColor: 'blue'
                            }]
                        },
                        options: {
                            scaleShowValues: true,
                            scales: {
                                yAxes: [{
                                    ticks: { beginAtZero: true}
                                }],
                                xAxes: [{
                                    ticks: {autoSkip: false }
                                }]
                            }
                        }
                    });
                }
            });
        }

        var GetPostsByUser = function() {

            $.ajax({
                type: "POST",
                url: "/Charts/PostsByUser",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (chData) {
                    var aData = chData;
                    var aLabels = aData[0];
                    var aDatasets1 = aData[1];

                    var ctx = $("#chart_posts");

                    var postsChart = new Chart(ctx, {
                        type: 'bar',
                        autoSkip: false,
                        animation:false,
                        showDataPoints:true,
                        responsive: true,
                        data: {
                            labels: aLabels,
                            datasets: [{
                                label: "Posts by User",
                                data: aDatasets1,
                                backgroundColor: 'blue'
                            }]
                        },
                        options: {
                            scaleShowValues: true,
                            scales: {
                                yAxes: [{
                                    ticks: { beginAtZero: true}
                                }],
                                xAxes: [{
                                    ticks: {autoSkip: false }
                                }]
                            }
                        }
                    });
                }
            });
        }

        var GetPostsByMonth = function(year) {

            $.ajax({
                type: "POST",
                url: "/Charts/PostsByMonth/" + year,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                error: function (data){
                    console.log(data);
                },
                success: function (chData) {
                    console.log(chData);
                    var aData = chData;
                    var aLabels = aData[0];
                    var aDatasets1 = aData[1];

                    var ctx = $("#chart_months");

                    var monthChart = new Chart(ctx, {
                        type: 'bar',
                        autoSkip: false,
                        animation:false,
                        showDataPoints:true,
                        responsive: true,
                        data: {
                            labels: aLabels,
                            datasets: [{
                                label: "Posts by Month",
                                data: aDatasets1,
                                backgroundColor: 'blue'
                            }]
                        },
                        options: {
                            scaleShowValues: true,
                            scales: {
                                yAxes: [{
                                    ticks: { beginAtZero: true}
                                }],
                                xAxes: [{
                                    ticks: {autoSkip: false }
                                }]
                            }
                        }
                    });
                }
            });
        }

        var GetPostsByYear = function () {

            $.ajax({
                type: "POST",
                url: "/Charts/PostsByYear",
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (chData) {
                    var aData = chData;
                    var aLabels = aData[0];
                    var aDatasets1 = aData[1];

                    var ctx = $("#chart_years");
                    var canvas = document.getElementById("chart_years");
                    var yearChart = new Chart(ctx, {
                        type: 'bar',
                        autoSkip: false,
                        animation: false,
                        showDataPoints: true,
                        responsive: true,
                        data: {
                            labels: aLabels,
                            datasets: [{
                                label: "Posts by Year",
                                data: aDatasets1,
                                backgroundColor: 'blue'
                            }]
                        },
                        options: {
                            scaleShowValues: true,
                            scales: {
                                yAxes: [{
                                    ticks: { beginAtZero: true }
                                }],
                                xAxes: [{
                                    ticks: { autoSkip: false }
                                }]
                            }
                        }
                    });
                    canvas.onclick = function (evt) {
                        var activePoints = yearChart.getElementsAtEvent(evt);
                        if (activePoints[0]) {
                            var chartData = activePoints[0]['_chart'].config.data;
                            var idx = activePoints[0]['_index'];
                            var label = chartData.labels[idx];
                            if ($("#year-select")) {
                                $("#year-select").val(label);
                            }
                            GetPostsByMonth(label);
                        }
                    };
                }
            });

        }